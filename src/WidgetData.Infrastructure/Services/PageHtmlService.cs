using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Web;
using WidgetData.Application.DTOs;
using WidgetData.Application.Helpers;
using WidgetData.Application.Interfaces;

namespace WidgetData.Infrastructure.Services;

/// <summary>
/// Assembles fully rendered HTML pages by:
///   1. Loading a WidgetGroup or PageDto (page definition) from the database.
///   2. Fetching live data for every widget in the page.
///   3. Rendering each widget's HTML template with its data via <see cref="HtmlTemplateHelper"/>.
///   4. Wrapping the result in a responsive grid and optionally in a standalone HTML document.
///   5. Optionally packaging multiple pages into a ZIP or a SPA index.html.
/// </summary>
public class PageHtmlService : IPageHtmlService
{
    private readonly IWidgetGroupService _groupService;
    private readonly IWidgetService _widgetService;

    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public PageHtmlService(IWidgetGroupService groupService, IWidgetService widgetService)
    {
        _groupService = groupService;
        _widgetService = widgetService;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<string> BuildAsync(int pageId, bool standalone = true, string? cssUrl = null)
    {
        var group = await _groupService.GetByIdAsync(pageId)
            ?? throw new KeyNotFoundException($"Page {pageId} not found.");

        var tasks = group.WidgetIds.Select(LoadWidgetWithDataAsync);
        var items = (await Task.WhenAll(tasks))
            .Where(x => x.Widget != null)
            .ToList();

        var gridHtml = BuildGrid(group.Name, items);
        return standalone ? BuildDocument(group.Name, gridHtml, cssUrl) : gridHtml;
    }

    public async Task<string> BuildFromPageAsync(PageDto page, bool standalone = true, string? cssUrl = null)
    {
        var orderedWidgets = page.Widgets.OrderBy(w => w.Position).ToList();

        var tasks = orderedWidgets.Select(async pw =>
        {
            WidgetDataDto? data = null;
            var raw = await _widgetService.GetDataAsync(pw.WidgetId);
            if (raw != null)
            {
                var json = JsonSerializer.Serialize(raw, _json);
                data = JsonSerializer.Deserialize<WidgetDataDto>(json, _json);
            }

            // Build a WidgetDto-like view from PageWidgetDto fields
            var widget = new WidgetDto
            {
                Id = pw.WidgetId,
                Name = pw.WidgetName,
                FriendlyLabel = pw.FriendlyLabel,
                HtmlTemplate = pw.HtmlTemplate,
            };

            return (Widget: (WidgetDto?)widget, Data: data);
        });

        var items = (await Task.WhenAll(tasks)).ToList();
        var gridHtml = BuildGrid(page.Title, items);
        return standalone ? BuildDocument(page.Title, gridHtml, cssUrl) : gridHtml;
    }

    public async Task<byte[]> BuildMultiPageZipAsync(IList<PageDto> pages)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var page in pages)
            {
                var html = await BuildFromPageAsync(page, standalone: true);
                var entryName = $"{SanitizeSlug(page.Slug)}.html";
                var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                await entryStream.WriteAsync(Encoding.UTF8.GetBytes(html));
            }
        }
        return ms.ToArray();
    }

    public async Task<string> BuildSpaHtmlAsync(IList<PageDto> pages)
    {
        var sb = new StringBuilder();

        // Build nav links
        var navLinks = new StringBuilder();
        foreach (var p in pages)
        {
            var label = HttpUtility.HtmlEncode(p.Title);
            var id = HttpUtility.HtmlAttributeEncode(SanitizeSlug(p.Slug));
            navLinks.AppendLine($"    <a href=\"#{id}\" class=\"spa-nav-link\" data-page=\"{id}\">{label}</a>");
        }

        // Build page sections (inner grid only, no full document wrapper)
        var sections = new StringBuilder();
        bool first = true;
        foreach (var p in pages)
        {
            var id = HttpUtility.HtmlAttributeEncode(SanitizeSlug(p.Slug));
            var gridHtml = await BuildFromPageAsync(p, standalone: false);
            var display = first ? "block" : "none";
            sections.AppendLine($"  <section id=\"{id}\" class=\"spa-section\" style=\"display:{display}\">");
            sections.Append(gridHtml);
            sections.AppendLine("  </section>");
            first = false;
        }

        var firstId = pages.Count > 0 ? SanitizeSlug(pages[0].Slug) : "";

        return $$"""
            <!DOCTYPE html>
            <html lang="vi">
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width, initial-scale=1.0">
              <title>WidgetData Site</title>
              <style>
            {{EmbeddedCss}}
            .spa-nav{background:#1a202c;padding:0 24px;display:flex;gap:4px;flex-wrap:wrap}
            .spa-nav-link{display:inline-block;padding:12px 18px;color:#e2e8f0;text-decoration:none;font-size:14px;font-weight:500;border-bottom:3px solid transparent}
            .spa-nav-link:hover{color:#fff;background:rgba(255,255,255,.06)}
            .spa-nav-link.active{color:#63b3ed;border-bottom-color:#63b3ed}
            .spa-section{max-width:1280px;margin:0 auto;padding:28px 24px}
              </style>
            </head>
            <body>
            <nav class="spa-nav">
            {{navLinks}}
            </nav>
            {{sections}}
            <script>
            (function(){
              function show(hash){
                var id = hash ? hash.replace(/^#/,'') : '{{firstId}}';
                document.querySelectorAll('.spa-section').forEach(function(s){ s.style.display='none'; });
                document.querySelectorAll('.spa-nav-link').forEach(function(a){ a.classList.remove('active'); });
                var sec = document.getElementById(id);
                if(sec){ sec.style.display='block'; }
                var link = document.querySelector('.spa-nav-link[data-page="'+id+'"]');
                if(link){ link.classList.add('active'); }
              }
              window.addEventListener('hashchange', function(){ show(location.hash); });
              show(location.hash);
            })();
            </script>
            </body>
            </html>
            """;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<(WidgetDto? Widget, WidgetDataDto? Data)> LoadWidgetWithDataAsync(int widgetId)
    {
        var widget = await _widgetService.GetByIdAsync(widgetId);
        if (widget == null) return (null, null);

        WidgetDataDto? data = null;
        var raw = await _widgetService.GetDataAsync(widgetId);
        if (raw != null)
        {
            var json = JsonSerializer.Serialize(raw, _json);
            data = JsonSerializer.Deserialize<WidgetDataDto>(json, _json);
        }

        return (widget, data);
    }

    private static string BuildGrid(string pageTitle, IList<(WidgetDto? Widget, WidgetDataDto? Data)> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<h1 class=\"we-page-title\">{HttpUtility.HtmlEncode(pageTitle)}</h1>");
        sb.AppendLine("<div class=\"we-layout-grid\" style=\"--we-columns: 3;\">");

        foreach (var (widget, data) in items)
        {
            if (widget == null) continue;

            var cols = data?.Columns?.ToList() ?? new List<string>();
            var rows = HtmlTemplateHelper.ToStringRows(data?.Rows);
            var tpl = !string.IsNullOrWhiteSpace(widget.HtmlTemplate)
                ? widget.HtmlTemplate
                : BuildDefaultTableTemplate(cols);

            var widgetHtml = HtmlTemplateHelper.Render(tpl, cols, rows);
            var title = HttpUtility.HtmlEncode(widget.FriendlyLabel ?? widget.Name);

            if (!string.IsNullOrEmpty(data?.Error))
            {
                widgetHtml = $"<div class=\"we-error\"><strong>Error</strong>: {HttpUtility.HtmlEncode(data.Error)}</div>";
            }

            sb.AppendLine("  <div class=\"we-cell\">");
            sb.AppendLine("    <div class=\"we-widget\">");
            sb.AppendLine($"      <div class=\"we-title\">{title}</div>");
            sb.AppendLine($"      <div class=\"we-body\">{widgetHtml}</div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("  </div>");
        }

        sb.AppendLine("</div>");
        return sb.ToString();
    }

    private static string BuildDefaultTableTemplate(IList<string> columns)
    {
        if (columns.Count == 0) return "<p class=\"we-empty\">No data</p>";

        var headers = string.Join("", columns.Select(c => $"<th>{HttpUtility.HtmlEncode(c)}</th>"));
        var cells = string.Join("", columns.Select(c => "<td>{{" + c + "}}</td>"));

        return "<table class=\"we-table\">"
             + "<thead><tr>" + headers + "</tr></thead>"
             + "<tbody>{{#each rows}}<tr>" + cells + "</tr>{{/each}}</tbody>"
             + "</table>";
    }

    private static string BuildDocument(string title, string bodyContent, string? cssUrl)
    {
        var cssTag = string.IsNullOrWhiteSpace(cssUrl)
            ? $"<style>{EmbeddedCss}</style>"
            : $"<link rel=\"stylesheet\" href=\"{HttpUtility.HtmlAttributeEncode(cssUrl)}\">";

        return $"""
            <!DOCTYPE html>
            <html lang="vi">
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width, initial-scale=1.0">
              <title>{HttpUtility.HtmlEncode(title)}</title>
              {cssTag}
            </head>
            <body>
            <main style="max-width:1280px;margin:0 auto;padding:28px 24px;">
            {bodyContent}
            </main>
            </body>
            </html>
            """;
    }

    private static string SanitizeSlug(string slug)
    {
        // Keep only alphanumeric, hyphens, underscores; fallback to "page"
        var clean = new string(slug.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
        return string.IsNullOrEmpty(clean) ? "page" : clean;
    }

    // ── Embedded CSS ─────────────────────────────────────────────────────────
    // Minimal subset of widget-engine.css so standalone pages look correct
    // even without a CDN or external file reference.

    private const string EmbeddedCss = """
        *,*::before,*::after{box-sizing:border-box}
        body{margin:0;padding:0;font-family:system-ui,-apple-system,sans-serif;background:#f7fafc;color:#1a202c}
        .we-page-title{font-size:22px;font-weight:700;color:#1a202c;margin:0 0 20px}
        .we-layout-grid{display:grid;grid-template-columns:repeat(var(--we-columns,3),1fr);gap:16px}
        @media(max-width:768px){.we-layout-grid{grid-template-columns:1fr}}
        .we-cell{min-width:0}
        .we-widget{background:#fff;border:1px solid #e2e8f0;border-radius:8px;padding:16px;font-family:system-ui,-apple-system,sans-serif;font-size:14px;color:#1a202c;overflow:auto}
        .we-title{font-size:15px;font-weight:600;margin-bottom:12px;color:#2d3748;border-bottom:1px solid #e2e8f0;padding-bottom:8px}
        .we-body{overflow:auto}
        .we-table{width:100%;border-collapse:collapse;font-size:13px}
        .we-table th{text-align:left;background:#f7fafc;color:#4a5568;font-weight:600;padding:8px 10px;border-bottom:2px solid #e2e8f0;white-space:nowrap}
        .we-table td{padding:7px 10px;border-bottom:1px solid #edf2f7;vertical-align:top}
        .we-table tr:last-child td{border-bottom:none}
        .we-table tr:hover td{background:#f7fafc}
        .we-error{color:#c53030;background:#fff5f5;border:1px solid #feb2b2;border-radius:4px;padding:10px 14px}
        .we-empty{color:#a0aec0;text-align:center;padding:24px 0}
        """;
}
