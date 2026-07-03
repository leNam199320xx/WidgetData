using Renci.SshNet;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;

namespace WidgetData.Infrastructure.Services;

public class SshDeliveryChannelStrategy : IDeliveryChannelStrategy
{
    public DeliveryType SupportedType => DeliveryType.Ssh;

    public async Task DeliverAsync(int widgetId, DeliveryTarget target, IExportService exportService, IHttpClientFactory httpClientFactory)
    {
        var cfg = ParseConfig(target.Configuration);
        var host = cfg.GetValueOrDefault("host", "");
        var username = cfg.GetValueOrDefault("username", "");
        if (string.IsNullOrWhiteSpace(host)) throw new InvalidOperationException("SSH delivery requires 'host' in configuration.");
        if (string.IsNullOrWhiteSpace(username)) throw new InvalidOperationException("SSH delivery requires 'username' in configuration.");

        var format = cfg.GetValueOrDefault("format", "csv");
        var data = await exportService.ExportAsync(widgetId, format);
        var fileName = exportService.GetFileName(widgetId, format);

        var port = int.Parse(cfg.GetValueOrDefault("port", "22"));
        var password = cfg.GetValueOrDefault("password", "");
        var remotePath = cfg.GetValueOrDefault("path", $"/tmp/{fileName}");

        using var client = new SftpClient(host, port, username, password);
        client.Connect();
        using var inputStream = new MemoryStream(data);
        client.UploadFile(inputStream, remotePath);
        client.Disconnect();
        await Task.CompletedTask;
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