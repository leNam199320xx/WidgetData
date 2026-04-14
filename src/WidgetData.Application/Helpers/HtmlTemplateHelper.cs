using System.Text;
using System.Text.Json;
using System.Web;

namespace WidgetData.Application.Helpers;

/// <summary>
/// Helper for rendering HTML templates with widget data variable substitution.
/// Templates support {{column_name}} for scalar values and {{#each rows}}...{{/each}} for iteration.
/// </summary>
public static class HtmlTemplateHelper
{
    public const int MaxPreviewRows = 100;
    public const int MaxBuilderPreviewRows = 20;

    /// <summary>
    /// Renders an HTML template by substituting column variable placeholders with actual row data.
    /// </summary>
    public static string Render(string template, IList<string> columns, IList<Dictionary<string, string>> rows)
    {
        if (string.IsNullOrWhiteSpace(template)) return string.Empty;

        var result = template;
        const string startTag = "{{#each rows}}";
        const string endTag = "{{/each}}";

        var startIdx = result.IndexOf(startTag, StringComparison.Ordinal);
        var endIdx = result.IndexOf(endTag, StringComparison.Ordinal);

        if (startIdx >= 0 && endIdx > startIdx)
        {
            var before = result[..startIdx];
            var rowTemplate = result[(startIdx + startTag.Length)..endIdx];
            var after = result[(endIdx + endTag.Length)..];

            var rowsHtml = new StringBuilder();
            foreach (var row in rows.Take(MaxPreviewRows))
            {
                var rowHtml = rowTemplate;
                foreach (var col in columns)
                {
                    var val = row.GetValueOrDefault(col) ?? string.Empty;
                    rowHtml = rowHtml.Replace($"{{{{{col}}}}}", HttpUtility.HtmlEncode(val));
                }
                rowsHtml.Append(rowHtml);
            }
            result = before + rowsHtml + after;
        }

        var firstRow = rows.FirstOrDefault();
        if (firstRow != null)
        {
            foreach (var col in columns)
            {
                var val = firstRow.GetValueOrDefault(col) ?? string.Empty;
                result = result.Replace($"{{{{{col}}}}}", HttpUtility.HtmlEncode(val));
            }
        }

        return result;
    }

    /// <summary>
    /// Converts raw object rows (from API deserialization) to string rows suitable for template rendering.
    /// </summary>
    public static List<Dictionary<string, string>> ToStringRows(IList<Dictionary<string, object?>>? rows)
    {
        if (rows == null) return new List<Dictionary<string, string>>();
        return rows
            .Select(r => r.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value is JsonElement je ? je.ToString() : kvp.Value?.ToString() ?? string.Empty))
            .ToList();
    }
}
