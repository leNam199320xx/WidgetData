using Microsoft.EntityFrameworkCore;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data;

namespace WidgetData.Infrastructure.Services;

public class WidgetActivityService : IWidgetActivityService
{
    private readonly IWidgetActivityRepository _activityRepo;
    private readonly IWidgetRepository _widgetRepo;
    private readonly ApplicationDbContext _context;

    public WidgetActivityService(
        IWidgetActivityRepository activityRepo,
        IWidgetRepository widgetRepo,
        ApplicationDbContext context)
    {
        _activityRepo = activityRepo;
        _widgetRepo = widgetRepo;
        _context = context;
    }

    public async Task RecordAsync(int widgetId, string apiEndpoint, string? userId, long? responseTimeMs, int statusCode)
    {
        var activity = new WidgetApiActivity
        {
            WidgetId = widgetId,
            ApiEndpoint = apiEndpoint,
            UserId = userId,
            CalledAt = DateTime.UtcNow,
            ResponseTimeMs = responseTimeMs,
            StatusCode = statusCode
        };
        await _activityRepo.RecordAsync(activity);

        // Update LastActivityAt on widget
        var widget = await _context.Widgets.FindAsync(widgetId);
        if (widget != null)
        {
            widget.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<PagedResult<WidgetActivityDto>> GetActivityAsync(int widgetId, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;

        var total = await _activityRepo.CountByWidgetIdAsync(widgetId);
        var items = await _activityRepo.GetByWidgetIdAsync(widgetId, page, pageSize);

        return new PagedResult<WidgetActivityDto>
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Items = items.Select(MapToDto).ToList()
        };
    }

    public async Task<WidgetActivitySummaryDto?> GetSummaryAsync(int widgetId)
    {
        var widget = await _widgetRepo.GetByIdAsync(widgetId);
        if (widget == null) return null;

        var activities = (await _activityRepo.GetSummaryDataAsync(widgetId)).ToList();

        var topEndpoints = activities
            .GroupBy(a => a.ApiEndpoint)
            .Select(g => new EndpointCallCountDto { ApiEndpoint = g.Key, CallCount = g.Count() })
            .OrderByDescending(e => e.CallCount)
            .Take(5)
            .ToList();

        return new WidgetActivitySummaryDto
        {
            WidgetId = widgetId,
            WidgetName = widget.Name,
            TotalCalls = activities.Count,
            UniqueUsers = activities.Where(a => a.UserId != null).Select(a => a.UserId).Distinct().Count(),
            LastActivityAt = widget.LastActivityAt,
            TopEndpoints = topEndpoints
        };
    }

    public async Task<IEnumerable<InactivityAlertDto>> GetInactiveWidgetsAsync(int thresholdDays)
    {
        var inactiveWidgets = await _activityRepo.GetInactiveWidgetsAsync(thresholdDays);
        return inactiveWidgets.Select(w => BuildAlertDto(w, thresholdDays, wasAutoDisabled: false));
    }

    public async Task<IEnumerable<InactivityAlertDto>> GetInactivityAlertsAsync()
    {
        var alerts = await _context.AuditLogs
            .Where(a => a.Action == "InactivityAlert")
            .OrderByDescending(a => a.Timestamp)
            .Take(200)
            .ToListAsync();

        return alerts.Select(a =>
        {
            var wasAutoDisabled = a.Notes?.Contains("AutoDisabled=true", StringComparison.OrdinalIgnoreCase) ?? false;
            _ = int.TryParse(a.EntityId, out var widgetId);
            _ = int.TryParse(
                System.Text.RegularExpressions.Regex.Match(a.Notes ?? "", @"DaysSinceLastActivity=(\d+)").Groups[1].Value,
                out var days);
            return new InactivityAlertDto
            {
                WidgetId = widgetId,
                WidgetName = a.Notes != null
                    ? System.Text.RegularExpressions.Regex.Match(a.Notes, @"WidgetName=([^;]+)").Groups[1].Value
                    : string.Empty,
                DaysSinceLastActivity = days,
                WasAutoDisabled = wasAutoDisabled,
                DetectedAt = a.Timestamp
            };
        });
    }

    private static InactivityAlertDto BuildAlertDto(Domain.Entities.Widget w, int thresholdDays, bool wasAutoDisabled)
    {
        var days = w.LastActivityAt.HasValue
            ? (int)(DateTime.UtcNow - w.LastActivityAt.Value).TotalDays
            : thresholdDays;
        return new InactivityAlertDto
        {
            WidgetId = w.Id,
            WidgetName = w.Name,
            DaysSinceLastActivity = days,
            WasAutoDisabled = wasAutoDisabled,
            DetectedAt = DateTime.UtcNow
        };
    }

    private static WidgetActivityDto MapToDto(WidgetApiActivity a) => new()
    {
        Id = a.Id,
        WidgetId = a.WidgetId,
        ApiEndpoint = a.ApiEndpoint,
        UserId = a.UserId,
        CalledAt = a.CalledAt,
        ResponseTimeMs = a.ResponseTimeMs,
        StatusCode = a.StatusCode
    };
}
