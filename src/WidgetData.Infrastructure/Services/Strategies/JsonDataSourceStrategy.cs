using System.Text.Json;
using Microsoft.Extensions.Logging;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Services;

namespace WidgetData.Infrastructure.Services;

public class JsonDataSourceStrategy : IDataSourceStrategy
{
    public bool CanHandle(DataSourceType sourceType) => sourceType == DataSourceType.Json;

    public Task<object> LoadDataAsync(Widget widget, DataSource ds, IHttpClientFactory httpClientFactory)
    {
        var filePath = ds.FileStoragePath ?? ds.ConnectionString;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return Task.FromResult<object>(new { error = $"JSON file not found: {filePath}" });

        var jsonText = File.ReadAllText(filePath);
        using var doc = System.Text.Json.JsonDocument.Parse(jsonText);
        var root = doc.RootElement;

        var config = string.IsNullOrWhiteSpace(widget.Configuration) ? null
            : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(widget.Configuration);
        var jsonPath = config?.GetValueOrDefault("jsonPath")?.ToString();

        System.Text.Json.JsonElement arrayEl = root;
        if (!string.IsNullOrWhiteSpace(jsonPath))
        {
            foreach (var segment in jsonPath.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                if (arrayEl.ValueKind != System.Text.Json.JsonValueKind.Object
                    || !arrayEl.TryGetProperty(segment, out var child))
                {
                    return Task.FromResult<object>(new
                    {
                        error = $"JSON path '{jsonPath}' is invalid. Segment '{segment}' was not found."
                    });
                }
                arrayEl = child;
            }
        }
        else if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            var foundArray = false;
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Value.ValueKind != System.Text.Json.JsonValueKind.Array) continue;
                arrayEl = prop.Value;
                foundArray = true;
                break;
            }

            if (!foundArray)
            {
                return Task.FromResult<object>(new
                {
                    error = "JSON root is an object and no array field was found. Configure `jsonPath` in widget Configuration, e.g. {\"jsonPath\":\"items\"}."
                });
            }
        }

        if (arrayEl.ValueKind != System.Text.Json.JsonValueKind.Array)
            return Task.FromResult<object>(new
            {
                error = "JSON root/jsonPath does not point to an array. Set widget Configuration `jsonPath` to an array field."
            });

        var rows = new List<Dictionary<string, object?>>();
        var columns = new List<string>();

        foreach (var item in arrayEl.EnumerateArray())
        {
            if (item.ValueKind != System.Text.Json.JsonValueKind.Object) continue;
            var row = new Dictionary<string, object?>();
            foreach (var prop in item.EnumerateObject())
            {
                if (!columns.Contains(prop.Name)) columns.Add(prop.Name);
                row[prop.Name] = prop.Value.ValueKind == System.Text.Json.JsonValueKind.String
                    ? prop.Value.GetString()
                    : prop.Value.ToString();
            }
            rows.Add(row);
        }
        return Task.FromResult<object>(new { columns, rows });
    }
}