using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WidgetData.Application.DTOs;
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

    public async Task<IEnumerable<AuditLogDto>> GetLogsAsync(
        int page = 1, int pageSize = 50,
        string? action = null, string? entityType = null, string? userEmail = null,
        DateTime? from = null, DateTime? to = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(l => l.Action.Contains(action));
        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(l => l.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(userEmail))
            query = query.Where(l => l.UserEmail != null && l.UserEmail.Contains(userEmail));
        if (from.HasValue)
            query = query.Where(l => l.Timestamp >= from.Value);
        if (to.HasValue)
            query = query.Where(l => l.Timestamp <= to.Value);

        var logs = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return logs.Select(MapToDto);
    }

    public async Task<int> CountLogsAsync(
        string? action = null, string? entityType = null, string? userEmail = null,
        DateTime? from = null, DateTime? to = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(l => l.Action.Contains(action));
        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(l => l.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(userEmail))
            query = query.Where(l => l.UserEmail != null && l.UserEmail.Contains(userEmail));
        if (from.HasValue)
            query = query.Where(l => l.Timestamp >= from.Value);
        if (to.HasValue)
            query = query.Where(l => l.Timestamp <= to.Value);

        return await query.CountAsync();
    }

    private static AuditLogDto MapToDto(AuditLog l) => new()
    {
        Id = l.Id,
        UserId = l.UserId,
        UserEmail = l.UserEmail,
        Action = l.Action,
        EntityType = l.EntityType,
        EntityId = l.EntityId,
        OldValues = l.OldValues,
        NewValues = l.NewValues,
        IpAddress = l.IpAddress,
        UserAgent = l.UserAgent,
        Timestamp = l.Timestamp,
        Notes = l.Notes
    };
}
