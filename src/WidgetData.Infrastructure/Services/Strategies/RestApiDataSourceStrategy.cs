using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Services;

namespace WidgetData.Infrastructure.Services;

public class RestApiDataSourceStrategy : IDataSourceStrategy
{
    public bool CanHandle(DataSourceType sourceType) => sourceType == DataSourceType.RestApi;

    public async Task<object> LoadDataAsync(Widget widget, DataSource ds, IHttpClientFactory httpClientFactory)
    {
        var endpoint = ds.ApiEndpoint;
        if (string.IsNullOrWhiteSpace(endpoint))
            return new { error = "API endpoint is not configured." };

        var config = string.IsNullOrWhiteSpace(widget.Configuration) ? null
            : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(widget.Configuration);
        var jsonPath = config?.GetValueOrDefault("jsonPath")?.ToString();

        var client = httpClientFactory.CreateClient();
        if (!string.IsNullOrWhiteSpace(ds.ApiKey))
            client.DefaultRequestHeaders.Add("X-Api-Key", ds.ApiKey);

        var response = await client.GetAsync(endpoint);
        if (!response.IsSuccessStatusCode)
            return new { error = $"API returned HTTP {(int)response.StatusCode}: {response.ReasonPhrase}" };

        var body = await response.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(body);
        var root = doc.RootElement;

        System.Text.Json.JsonElement arrayEl = root;
        if (!string.IsNullOrWhiteSpace(jsonPath))
        {
            foreach (var segment in jsonPath.Split('.', StringSplitOptions.RemoveEmptyEntries))
                if (arrayEl.TryGetProperty(segment, out var child))
                    arrayEl = child;
        }

        if (arrayEl.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
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
            return new { columns, rows };
        }

        if (arrayEl.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            var columns = new List<string>();
            var row = new Dictionary<string, object?>();
            foreach (var prop in arrayEl.EnumerateObject())
            {
                columns.Add(prop.Name);
                row[prop.Name] = prop.Value.ValueKind == System.Text.Json.JsonValueKind.String
                    ? prop.Value.GetString()
                    : prop.Value.ToString();
            }
            return new { columns, rows = new List<Dictionary<string, object?>> { row } };
        }

        return new { error = "API response is not a JSON array or object." };
    }
}