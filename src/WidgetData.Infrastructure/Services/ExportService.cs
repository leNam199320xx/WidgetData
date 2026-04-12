using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WidgetData.Application.Interfaces;
using WidgetData.Infrastructure.Data;

namespace WidgetData.Infrastructure.Services;

public class ExportService : IExportService
{
    private readonly ApplicationDbContext _context;
    private readonly IWidgetService _widgetService;

    public ExportService(ApplicationDbContext context, IWidgetService widgetService)
    {
        _context = context;
        _widgetService = widgetService;
    }

    public async Task<byte[]> ExportAsync(int widgetId, string format)
    {
        var widget = await _widgetService.GetByIdAsync(widgetId)
            ?? throw new KeyNotFoundException($"Widget {widgetId} not found");

        // Get sample data from executions for export
        var executions = await _context.WidgetExecutions
            .Where(e => e.WidgetId == widgetId)
            .OrderByDescending(e => e.StartedAt)
            .Take(100)
            .ToListAsync();

        var headers = new[] { "ExecutionId", "Status", "TriggeredBy", "StartedAt", "CompletedAt", "ExecutionTimeMs", "RowCount", "ErrorMessage" };
        var rows = executions.Select(e => new object?[]
        {
            e.ExecutionId, e.Status.ToString(), e.TriggeredBy.ToString(),
            e.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            e.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
            e.ExecutionTimeMs, e.RowCount, e.ErrorMessage
        }).ToList();

        return format.ToLower() switch
        {
            "csv" => ExportCsv(headers, rows),
            "txt" => ExportTxt(widget.Name, headers, rows),
            "excel" or "xlsx" => ExportExcel(widget.Name, headers, rows),
            "pdf" => ExportPdf(widget.Name, headers, rows),
            "html" => ExportHtml(widget.Name, headers, rows),
            _ => throw new NotSupportedException($"Format '{format}' is not supported")
        };
    }

    public string GetContentType(string format) => format.ToLower() switch
    {
        "csv" => "text/csv",
        "txt" => "text/plain",
        "excel" or "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "pdf" => "application/pdf",
        "html" => "text/html",
        _ => "application/octet-stream"
    };

    public string GetFileName(int widgetId, string format)
    {
        var ext = format.ToLower() switch
        {
            "excel" => "xlsx",
            _ => format.ToLower()
        };
        return $"widget_{widgetId}_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{ext}";
    }

    private static byte[] ExportCsv(string[] headers, List<object?[]> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
        foreach (var row in rows)
            sb.AppendLine(string.Join(",", row.Select(v => EscapeCsv(v?.ToString() ?? ""))));
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] ExportTxt(string title, string[] headers, List<object?[]> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Export: {title}");
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine(new string('-', 80));
        sb.AppendLine(string.Join("\t", headers));
        sb.AppendLine(new string('-', 80));
        foreach (var row in rows)
            sb.AppendLine(string.Join("\t", row.Select(v => v?.ToString() ?? "")));
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] ExportExcel(string title, string[] headers, List<object?[]> rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Export");

        // Title
        var titleCell = ws.Cell(1, 1);
        titleCell.Value = title;
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.FontSize = 14;
        ws.Range(1, 1, 1, headers.Length).Merge();

        // Headers
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(2, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.SteelBlue;
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Data
        for (var r = 0; r < rows.Count; r++)
        {
            for (var c = 0; c < rows[r].Length; c++)
            {
                var val = rows[r][c];
                var cell = ws.Cell(r + 3, c + 1);
                if (val is int intVal)
                    cell.Value = intVal;
                else if (val is long longVal)
                    cell.Value = longVal;
                else
                    cell.Value = val?.ToString() ?? "";
            }
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] ExportPdf(string title, string[] headers, List<object?[]> rows)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1, Unit.Centimetre);
                page.Header().Text(title).FontSize(16).Bold();
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        foreach (var _ in headers)
                            cols.RelativeColumn();
                    });
                    table.Header(header =>
                    {
                        foreach (var h in headers)
                        {
                            header.Cell().Background("#4682B4")
                                .Padding(4).Text(h).FontColor("#FFFFFF").Bold();
                        }
                    });
                    foreach (var row in rows)
                    {
                        foreach (var cell in row)
                        {
                            table.Cell().BorderBottom(0.5f, Unit.Point).BorderColor("#CCCCCC")
                                .Padding(4).Text(cell?.ToString() ?? "").FontSize(9);
                        }
                    }
                });
                page.Footer().AlignRight()
                    .Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").FontSize(8);
            });
        });
        return doc.GeneratePdf();
    }

    private static byte[] ExportHtml(string title, string[] headers, List<object?[]> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
        sb.AppendLine($"<title>{HtmlEncode(title)}</title>");
        sb.AppendLine("<style>body{font-family:sans-serif;margin:20px}table{border-collapse:collapse;width:100%}");
        sb.AppendLine("th{background:#4682B4;color:#fff;padding:8px;text-align:left}");
        sb.AppendLine("td{padding:6px 8px;border-bottom:1px solid #ddd}tr:nth-child(even){background:#f5f5f5}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine($"<h2>{HtmlEncode(title)}</h2>");
        sb.AppendLine($"<p>Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</p>");
        sb.AppendLine("<table><thead><tr>");
        foreach (var h in headers) sb.AppendLine($"<th>{HtmlEncode(h)}</th>");
        sb.AppendLine("</tr></thead><tbody>");
        foreach (var row in rows)
        {
            sb.AppendLine("<tr>");
            foreach (var cell in row) sb.AppendLine($"<td>{HtmlEncode(cell?.ToString() ?? "")}</td>");
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</tbody></table></body></html>");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string HtmlEncode(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
