using System.Net.Http.Headers;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;

namespace WidgetData.Infrastructure.Services;

public class HttpApiDeliveryChannelStrategy : IDeliveryChannelStrategy
{
    public DeliveryType SupportedType => DeliveryType.HttpApi;

    public async Task DeliverAsync(int widgetId, DeliveryTarget target, IExportService exportService, IHttpClientFactory httpClientFactory)
    {
        var cfg = ParseConfig(target.Configuration);
        var url = cfg.GetValueOrDefault("url", "");
        if (string.IsNullOrWhiteSpace(url)) throw new InvalidOperationException("HttpApi delivery requires 'url' in configuration.");

        var format = cfg.GetValueOrDefault("format", "csv");
        var data = await exportService.ExportAsync(widgetId, format);
        var fileName = exportService.GetFileName(widgetId, format);

        var method = cfg.GetValueOrDefault("method", "POST").ToUpper();
        var apiKey = cfg.GetValueOrDefault("apiKey", "");

        var http = httpClientFactory.CreateClient();
        if (!string.IsNullOrEmpty(apiKey))
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(data), "file", fileName);

        var response = method == "PUT"
            ? await http.PutAsync(url, content)
            : await http.PostAsync(url, content);

        response.EnsureSuccessStatusCode();
    }

    private static Dictionary<string, string> ParseConfig(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, string>();
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }
}