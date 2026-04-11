using System.Text.Json;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Infrastructure.Data;

namespace WidgetData.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;

    public AuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string action, string? entityType = null, string? entityId = null,
        object? oldValues = null, object? newValues = null,
        string? userId = null, string? userEmail = null,
        string? ipAddress = null, string? userAgent = null, string? notes = null)
    {
        var log = new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            UserId = userId,
            UserEmail = userEmail,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Notes = notes,
            Timestamp = DateTime.UtcNow
        };
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
