using Telegram.Bot;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;

namespace WidgetData.Delivery;

public class TelegramDeliveryChannelStrategy : IDeliveryChannelStrategy
{
    public DeliveryType SupportedType => DeliveryType.Telegram;

    public async Task DeliverAsync(int widgetId, DeliveryTarget target, IExportService exportService, IHttpClientFactory httpClientFactory)
    {
        var cfg = ParseConfig(target.Configuration);
        var botToken = cfg.GetValueOrDefault("botToken", "");
        var chatId = cfg.GetValueOrDefault("chatId", "");
        if (string.IsNullOrWhiteSpace(botToken)) throw new InvalidOperationException("Telegram delivery requires 'botToken' in configuration.");
        if (string.IsNullOrWhiteSpace(chatId)) throw new InvalidOperationException("Telegram delivery requires 'chatId' in configuration.");

        var format = cfg.GetValueOrDefault("format", "csv");
        var data = await exportService.ExportAsync(widgetId, format);
        var fileName = exportService.GetFileName(widgetId, format);
        var caption = cfg.GetValueOrDefault("caption", $"Widget {widgetId} export");

        var bot = new TelegramBotClient(botToken);
        using var stream = new MemoryStream(data);
        await bot.SendDocument(
            chatId: chatId,
            document: new Telegram.Bot.Types.InputFileStream(stream, fileName),
            caption: caption);
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
