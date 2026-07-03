using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;

namespace WidgetData.Infrastructure.Services;

public class FileDeliveryChannelStrategy : IDeliveryChannelStrategy
{
    public DeliveryType SupportedType
        => DeliveryType.Csv;

    public Task DeliverAsync(int widgetId, DeliveryTarget target, IExportService exportService, IHttpClientFactory httpClientFactory)
    {
        var cfg = ParseConfig(target.Configuration);
        var format = target.Type switch
        {
            DeliveryType.Csv => "csv",
            DeliveryType.Excel => "excel",
            DeliveryType.Pdf => "pdf",
            DeliveryType.HtmlFile => "html",
            DeliveryType.Txt => "txt",
            _ => "csv"
        };
        var data = exportService.ExportAsync(widgetId, format).GetAwaiter().GetResult();
        var fileName = exportService.GetFileName(widgetId, format);
        var outputPath = cfg.GetValueOrDefault("outputPath", Path.GetTempPath());
        var fullPath = Path.Combine(outputPath, fileName);
        File.WriteAllBytesAsync(fullPath, data).GetAwaiter().GetResult();
        return Task.CompletedTask;
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