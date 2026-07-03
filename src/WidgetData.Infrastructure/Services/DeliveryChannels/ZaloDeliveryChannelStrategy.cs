using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;

namespace WidgetData.Infrastructure.Services;

public class ZaloDeliveryChannelStrategy : IDeliveryChannelStrategy
{
    public DeliveryType SupportedType => DeliveryType.Zalo;

    public async Task DeliverAsync(int widgetId, DeliveryTarget target, IExportService exportService, IHttpClientFactory httpClientFactory)
    {
        var cfg = ParseConfig(target.Configuration);
        var accessToken = cfg.GetValueOrDefault("accessToken", "");
        var toUserId = cfg.GetValueOrDefault("toUserId", "");
        if (string.IsNullOrWhiteSpace(accessToken)) throw new InvalidOperationException("Zalo delivery requires 'accessToken' in configuration.");
        if (string.IsNullOrWhiteSpace(toUserId)) throw new InvalidOperationException("Zalo delivery requires 'toUserId' in configuration.");

        var format = cfg.GetValueOrDefault("format", "csv");
        var data = await exportService.ExportAsync(widgetId, format);
        var fileName = exportService.GetFileName(widgetId, format);
        var message = cfg.GetValueOrDefault("message", $"Widget {widgetId} export: {fileName}");

        var http = httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Add("access_token", accessToken);

        var payload = JsonSerializer.Serialize(new
        {
            recipient = new { user_id = toUserId },
            message = new { text = message }
        });

        var response = await http.PostAsync(
            "https://openapi.zalo.me/v3.0/oa/message/cs",
            new StringContent(payload, Encoding.UTF8, "application/json"));
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