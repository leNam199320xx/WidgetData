using Renci.SshNet;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;

namespace WidgetData.Delivery;

public class SftpDeliveryChannelStrategy : IDeliveryChannelStrategy
{
    public DeliveryType SupportedType => DeliveryType.Sftp;

    public async Task DeliverAsync(int widgetId, DeliveryTarget target, IExportService exportService, IHttpClientFactory httpClientFactory)
    {
        var cfg = ParseConfig(target.Configuration);
        var host = cfg.GetValueOrDefault("host", "");
        var username = cfg.GetValueOrDefault("username", "");
        if (string.IsNullOrWhiteSpace(host)) throw new InvalidOperationException("SFTP delivery requires 'host' in configuration.");
        if (string.IsNullOrWhiteSpace(username)) throw new InvalidOperationException("SFTP delivery requires 'username' in configuration.");

        var format = cfg.GetValueOrDefault("format", "csv");
        var data = await exportService.ExportAsync(widgetId, format);
        var fileName = exportService.GetFileName(widgetId, format);
        var remotePath = cfg.GetValueOrDefault("path", "/upload") + "/" + fileName;

        var port = int.Parse(cfg.GetValueOrDefault("port", "22"));
        var password = cfg.GetValueOrDefault("password", "");

        ConnectionInfo connInfo;
        var privateKeyPath = cfg.GetValueOrDefault("privateKeyPath", "");
        if (!string.IsNullOrEmpty(privateKeyPath) && File.Exists(privateKeyPath))
            connInfo = new ConnectionInfo(host, port, username, new PrivateKeyAuthenticationMethod(username, new PrivateKeyFile(privateKeyPath)));
        else
            connInfo = new ConnectionInfo(host, port, username, new PasswordAuthenticationMethod(username, password));

        using var sftp = new SftpClient(connInfo);
        sftp.Connect();
        using var stream = new MemoryStream(data);
        sftp.UploadFile(stream, remotePath);
        sftp.Disconnect();
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
