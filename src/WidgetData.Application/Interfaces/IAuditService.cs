namespace WidgetData.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(string action, string? entityType = null, string? entityId = null,
        object? oldValues = null, object? newValues = null,
        string? userId = null, string? userEmail = null,
        string? ipAddress = null, string? userAgent = null, string? notes = null);
}
