using System.Text;
using Microsoft.Extensions.Logging;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Services;

namespace WidgetData.Infrastructure.Services;

public class CsvDataSourceStrategy : IDataSourceStrategy
{
    public bool CanHandle(DataSourceType sourceType) => sourceType == DataSourceType.Csv;

    public Task<object> LoadDataAsync(Widget widget, DataSource ds, IHttpClientFactory httpClientFactory)
    {
        var filePath = ds.ConnectionString;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return Task.FromResult<object>(new { error = $"CSV file not found: {filePath}" });

        var config = string.IsNullOrWhiteSpace(widget.Configuration) ? null
            : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(widget.Configuration);
        var delimiter = config?.GetValueOrDefault("delimiter")?.ToString() ?? ",";
        var hasHeader = config?.GetValueOrDefault("hasHeader")?.ToString()?.ToLower() != "false";
        char sep = delimiter.Length == 1 ? delimiter[0] : ',';

        var lines = File.ReadAllLines(filePath);
        if (lines.Length == 0)
            return Task.FromResult<object>(new { columns = Array.Empty<string>(), rows = Array.Empty<object>() });

        List<string> columns;
        int startLine;
        if (hasHeader)
        {
            columns = lines[0].Split(sep).Select(c => c.Trim('"', ' ')).ToList();
            startLine = 1;
        }
        else
        {
            var firstRow = lines[0].Split(sep);
            columns = Enumerable.Range(0, firstRow.Length).Select(i => $"col{i + 1}").ToList();
            startLine = 0;
        }

        var rows = new List<Dictionary<string, object?>>();
        for (int i = startLine; i < lines.Length; i++)
        {
            var parts = SplitCsvLine(lines[i], sep);
            var row = new Dictionary<string, object?>();
            for (int c = 0; c < columns.Count; c++)
                row[columns[c]] = c < parts.Count ? parts[c] : null;
            rows.Add(row);
        }
        return Task.FromResult<object>(new { columns, rows });
    }

    private static List<string> SplitCsvLine(string line, char sep)
    {
        var result = new List<string>();
        var inQuotes = false;
        var current = new StringBuilder();
        for (int i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(ch);
                }
            }
            else
            {
                if (ch == '"') { inQuotes = true; }
                else if (ch == sep) { result.Add(current.ToString()); current.Clear(); }
                else { current.Append(ch); }
            }
        }
        result.Add(current.ToString());
        return result;
    }
}