using System.Net.Http.Headers;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;

namespace WidgetData.Infrastructure.Services;

public class EmailDeliveryChannelStrategy : IDeliveryChannelStrategy
{
    public DeliveryType SupportedType => DeliveryType.Email;

    public async Task DeliverAsync(int widgetId, DeliveryTarget target, IExportService exportService, IHttpClientFactory httpClientFactory)
    {
        var cfg = ParseConfig(target.Configuration);
        var format = cfg.GetValueOrDefault("format", "csv");
        var data = await exportService.ExportAsync(widgetId, format);
        var fileName = exportService.GetFileName(widgetId, format);

        var to = cfg.GetValueOrDefault("to", "");
        var cc = cfg.GetValueOrDefault("cc", "");
        if (string.IsNullOrWhiteSpace(to) && string.IsNullOrWhiteSpace(cc))
            throw new InvalidOperationException("Email delivery requires at least one recipient in 'to' or 'cc'.");

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(cfg.GetValueOrDefault("from", "noreply@widgetdata.local")));

        foreach (var addr in to.Split(';', StringSplitOptions.RemoveEmptyEntries))
            message.To.Add(MailboxAddress.Parse(addr.Trim()));

        foreach (var addr in cc.Split(';', StringSplitOptions.RemoveEmptyEntries))
            message.Cc.Add(MailboxAddress.Parse(addr.Trim()));

        message.Subject = cfg.GetValueOrDefault("subject", $"Widget {widgetId} export");
        var body = new BodyBuilder
        {
            TextBody = cfg.GetValueOrDefault("body", $"Widget export attached.\nGenerated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
        };
        body.Attachments.Add(fileName, data, ContentType.Parse(exportService.GetContentType(format)));
        message.Body = body.ToMessageBody();

        using var client = new SmtpClient();
        var host = cfg.GetValueOrDefault("host", "localhost");
        var port = int.Parse(cfg.GetValueOrDefault("port", "587"));
        var useSsl = bool.Parse(cfg.GetValueOrDefault("ssl", "true"));
        await client.ConnectAsync(host, port, useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

        var username = cfg.GetValueOrDefault("username", "");
        var password = cfg.GetValueOrDefault("password", "");
        if (!string.IsNullOrEmpty(username))
            await client.AuthenticateAsync(username, password);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
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