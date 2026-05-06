using WidgetData.Application.DTOs;

namespace WidgetData.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(string action, string? entityType = null, string? entityId = null,
        object? oldValues = null, object? newValues = null,
        string? userId = null, string? userEmail = null,
        string? ipAddress = null, string? userAgent = null, string? notes = null);

    Task<IEnumerable<AuditLogDto>> GetLogsAsync(
        int page = 1, int pageSize = 50,
        string? action = null, string? entityType = null, string? userEmail = null,
        DateTime? from = null, DateTime? to = null);

    Task<int> CountLogsAsync(
        string? action = null, string? entityType = null, string? userEmail = null,
        DateTime? from = null, DateTime? to = null);
}
