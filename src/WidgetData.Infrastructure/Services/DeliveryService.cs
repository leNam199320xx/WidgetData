using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using Renci.SshNet;
using Telegram.Bot;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Infrastructure.Data;

namespace WidgetData.Infrastructure.Services;

public class DeliveryService : IDeliveryService
{
    private readonly ApplicationDbContext _context;
    private readonly IExportService _exportService;
    private readonly IHttpClientFactory _httpClientFactory;

    public DeliveryService(
        ApplicationDbContext context,
        IExportService exportService,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _exportService = exportService;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IEnumerable<DeliveryTargetDto>> GetTargetsAsync(int widgetId)
    {
        var targets = await _context.DeliveryTargets
            .Where(t => t.WidgetId == widgetId)
            .ToListAsync();
        return targets.Select(MapToDto);
    }

    public async Task<DeliveryTargetDto?> GetTargetByIdAsync(int id)
    {
        var target = await _context.DeliveryTargets.FindAsync(id);
        return target == null ? null : MapToDto(target);
    }

    public async Task<DeliveryTargetDto> CreateTargetAsync(CreateDeliveryTargetDto dto, string userId)
    {
        var target = new DeliveryTarget
        {
            WidgetId = dto.WidgetId,
            Name = dto.Name,
            Type = dto.Type,
            Configuration = dto.Configuration,
            IsEnabled = dto.IsEnabled,
            CreatedBy = userId
        };
        _context.DeliveryTargets.Add(target);
        await _context.SaveChangesAsync();
        return MapToDto(target);
    }

    public async Task<DeliveryTargetDto?> UpdateTargetAsync(int id, UpdateDeliveryTargetDto dto)
    {
        var target = await _context.DeliveryTargets.FindAsync(id);
        if (target == null) return null;
        target.Name = dto.Name;
        target.Type = dto.Type;
        target.Configuration = dto.Configuration;
        target.IsEnabled = dto.IsEnabled;
        target.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return MapToDto(target);
    }

    public async Task<bool> DeleteTargetAsync(int id)
    {
        var target = await _context.DeliveryTargets.FindAsync(id);
        if (target == null) return false;
        _context.DeliveryTargets.Remove(target);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<DeliveryExecutionDto> DeliverAsync(int widgetId, int deliveryTargetId, string userId)
    {
        var target = await _context.DeliveryTargets.FindAsync(deliveryTargetId)
            ?? throw new KeyNotFoundException($"Delivery target {deliveryTargetId} not found");

        var execution = new DeliveryExecution
        {
            DeliveryTargetId = deliveryTargetId,
            Status = ExecutionStatus.Running,
            TriggeredBy = userId,
            ExecutedAt = DateTime.UtcNow
        };
        _context.DeliveryExecutions.Add(execution);
        await _context.SaveChangesAsync();

        try
        {
            await DispatchAsync(widgetId, target);
            execution.Status = ExecutionStatus.Success;
            execution.Message = $"Delivered via {target.Type} at {DateTime.UtcNow:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            execution.Status = ExecutionStatus.Failed;
            execution.Message = ex.Message;
        }

        await _context.SaveChangesAsync();
        return MapExecutionToDto(execution, target);
    }

    public async Task<IEnumerable<DeliveryExecutionDto>> GetExecutionsAsync(int widgetId)
    {
        var targetIds = await _context.DeliveryTargets
            .Where(t => t.WidgetId == widgetId)
            .Select(t => t.Id)
            .ToListAsync();

        var executions = await _context.DeliveryExecutions
            .Include(e => e.DeliveryTarget)
            .Where(e => targetIds.Contains(e.DeliveryTargetId))
            .OrderByDescending(e => e.ExecutedAt)
            .ToListAsync();

        return executions.Select(e => MapExecutionToDto(e, e.DeliveryTarget));
    }

    private async Task DispatchAsync(int widgetId, DeliveryTarget target)
    {
        switch (target.Type)
        {
            case DeliveryType.Email:
                await DeliverEmailAsync(widgetId, target);
                break;
            case DeliveryType.Sftp:
                await DeliverSftpAsync(widgetId, target);
                break;
            case DeliveryType.Ssh:
                await DeliverSshAsync(widgetId, target);
                break;
            case DeliveryType.HttpApi:
                await DeliverHttpApiAsync(widgetId, target);
                break;
            case DeliveryType.Telegram:
                await DeliverTelegramAsync(widgetId, target);
                break;
            case DeliveryType.Zalo:
                await DeliverZaloAsync(widgetId, target);
                break;
            case DeliveryType.Csv:
            case DeliveryType.Excel:
            case DeliveryType.Pdf:
            case DeliveryType.HtmlFile:
            case DeliveryType.Txt:
                // File-type deliveries: generate and save locally as configured
                await DeliverFileAsync(widgetId, target);
                break;
            default:
                throw new NotSupportedException($"Delivery type '{target.Type}' is not supported");
        }
    }

    private async Task DeliverEmailAsync(int widgetId, DeliveryTarget target)
    {
        var cfg = ParseConfig(target.Configuration);
        var format = cfg.GetValueOrDefault("format", "csv");
        var data = await _exportService.ExportAsync(widgetId, format);
        var fileName = _exportService.GetFileName(widgetId, format);

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(cfg.GetValueOrDefault("from", "noreply@widgetdata.local")));

        var to = cfg.GetValueOrDefault("to", "");
        foreach (var addr in to.Split(';', StringSplitOptions.RemoveEmptyEntries))
            message.To.Add(MailboxAddress.Parse(addr.Trim()));

        var cc = cfg.GetValueOrDefault("cc", "");
        foreach (var addr in cc.Split(';', StringSplitOptions.RemoveEmptyEntries))
            message.Cc.Add(MailboxAddress.Parse(addr.Trim()));

        message.Subject = cfg.GetValueOrDefault("subject", $"Widget {widgetId} export");

        var body = new BodyBuilder
        {
            TextBody = cfg.GetValueOrDefault("body", $"Widget export attached.\nGenerated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
        };
        body.Attachments.Add(fileName, data, ContentType.Parse(_exportService.GetContentType(format)));
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

    private async Task DeliverSftpAsync(int widgetId, DeliveryTarget target)
    {
        var cfg = ParseConfig(target.Configuration);
        var format = cfg.GetValueOrDefault("format", "csv");
        var data = await _exportService.ExportAsync(widgetId, format);
        var fileName = _exportService.GetFileName(widgetId, format);
        var remotePath = cfg.GetValueOrDefault("path", "/upload") + "/" + fileName;

        var host = cfg["host"];
        var port = int.Parse(cfg.GetValueOrDefault("port", "22"));
        var username = cfg["username"];
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

    private async Task DeliverSshAsync(int widgetId, DeliveryTarget target)
    {
        var cfg = ParseConfig(target.Configuration);
        var format = cfg.GetValueOrDefault("format", "csv");
        var data = await _exportService.ExportAsync(widgetId, format);
        var fileName = _exportService.GetFileName(widgetId, format);

        var host = cfg["host"];
        var port = int.Parse(cfg.GetValueOrDefault("port", "22"));
        var username = cfg["username"];
        var password = cfg.GetValueOrDefault("password", "");
        var remotePath = cfg.GetValueOrDefault("path", $"/tmp/{fileName}");

        using var client = new SftpClient(host, port, username, password);
        client.Connect();
        using var inputStream = new MemoryStream(data);
        client.UploadFile(inputStream, remotePath);
        client.Disconnect();
        await Task.CompletedTask;
    }

    private async Task DeliverHttpApiAsync(int widgetId, DeliveryTarget target)
    {
        var cfg = ParseConfig(target.Configuration);
        var format = cfg.GetValueOrDefault("format", "csv");
        var data = await _exportService.ExportAsync(widgetId, format);
        var fileName = _exportService.GetFileName(widgetId, format);

        var url = cfg["url"];
        var method = cfg.GetValueOrDefault("method", "POST").ToUpper();
        var apiKey = cfg.GetValueOrDefault("apiKey", "");

        var http = _httpClientFactory.CreateClient();
        if (!string.IsNullOrEmpty(apiKey))
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(data), "file", fileName);

        var response = method == "PUT"
            ? await http.PutAsync(url, content)
            : await http.PostAsync(url, content);

        response.EnsureSuccessStatusCode();
    }

    private async Task DeliverTelegramAsync(int widgetId, DeliveryTarget target)
    {
        var cfg = ParseConfig(target.Configuration);
        var format = cfg.GetValueOrDefault("format", "csv");
        var data = await _exportService.ExportAsync(widgetId, format);
        var fileName = _exportService.GetFileName(widgetId, format);

        var botToken = cfg["botToken"];
        var chatId = cfg["chatId"];
        var caption = cfg.GetValueOrDefault("caption", $"Widget {widgetId} export");

        var bot = new TelegramBotClient(botToken);
        using var stream = new MemoryStream(data);
        await bot.SendDocument(
            chatId: chatId,
            document: new Telegram.Bot.Types.InputFileStream(stream, fileName),
            caption: caption);
    }

    private async Task DeliverZaloAsync(int widgetId, DeliveryTarget target)
    {
        var cfg = ParseConfig(target.Configuration);
        var format = cfg.GetValueOrDefault("format", "csv");
        var data = await _exportService.ExportAsync(widgetId, format);
        var fileName = _exportService.GetFileName(widgetId, format);

        var accessToken = cfg["accessToken"];
        var toUserId = cfg["toUserId"];
        var message = cfg.GetValueOrDefault("message", $"Widget {widgetId} export: {fileName}");

        var http = _httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Add("access_token", accessToken);

        // Zalo OA send message (simplified: text message with file info)
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

    private async Task DeliverFileAsync(int widgetId, DeliveryTarget target)
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
        var data = await _exportService.ExportAsync(widgetId, format);
        var fileName = _exportService.GetFileName(widgetId, format);
        var outputPath = cfg.GetValueOrDefault("outputPath", Path.GetTempPath());
        var fullPath = Path.Combine(outputPath, fileName);
        await File.WriteAllBytesAsync(fullPath, data);
    }

    private static Dictionary<string, string> ParseConfig(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, string>();
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private static DeliveryTargetDto MapToDto(DeliveryTarget t) => new()
    {
        Id = t.Id,
        WidgetId = t.WidgetId,
        Name = t.Name,
        Type = t.Type,
        Configuration = t.Configuration,
        IsEnabled = t.IsEnabled,
        CreatedBy = t.CreatedBy,
        CreatedAt = t.CreatedAt
    };

    private static DeliveryExecutionDto MapExecutionToDto(DeliveryExecution e, DeliveryTarget? target) => new()
    {
        Id = e.Id,
        DeliveryTargetId = e.DeliveryTargetId,
        DeliveryTargetName = target?.Name,
        WidgetId = target?.WidgetId ?? 0,
        Status = e.Status,
        Message = e.Message,
        TriggeredBy = e.TriggeredBy,
        ExecutedAt = e.ExecutedAt
    };
}
