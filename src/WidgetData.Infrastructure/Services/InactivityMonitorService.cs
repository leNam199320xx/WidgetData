using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WidgetData.Domain.Entities;
using WidgetData.Infrastructure.Data;

namespace WidgetData.Infrastructure.Services;

public class InactivityMonitorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InactivityMonitorService> _logger;
    private readonly TimeSpan _checkInterval;
    private readonly int _defaultThresholdDays;

    public InactivityMonitorService(
        IServiceScopeFactory scopeFactory,
        ILogger<InactivityMonitorService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _checkInterval = TimeSpan.FromMinutes(
            configuration.GetValue<int>("InactivityMonitor:CheckIntervalMinutes", 60));
        _defaultThresholdDays = configuration.GetValue<int>("InactivityMonitor:DefaultThresholdDays", 30);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("InactivityMonitorService started. Check interval: {Interval}", _checkInterval);

        // Wait briefly on startup before the first check
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCheckAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during inactivity monitor check");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task RunCheckAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Load all active widgets that might be inactive — we filter per-widget threshold below
        var candidates = await context.Widgets
            .Where(w => w.IsActive)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var inactiveWidgets = candidates
            .Where(w =>
            {
                var threshold = w.InactivityThresholdDays > 0 ? w.InactivityThresholdDays : _defaultThresholdDays;
                return w.LastActivityAt == null || (now - w.LastActivityAt.Value).TotalDays >= threshold;
            })
            .ToList();

        if (!inactiveWidgets.Any())
        {
            _logger.LogDebug("InactivityMonitor: no inactive widgets found");
            return;
        }

        _logger.LogInformation("InactivityMonitor: found {Count} inactive widget(s)", inactiveWidgets.Count);

        foreach (var widget in inactiveWidgets)
        {
            var threshold = widget.InactivityThresholdDays > 0 ? widget.InactivityThresholdDays : _defaultThresholdDays;
            var daysSince = widget.LastActivityAt.HasValue
                ? (int)(now - widget.LastActivityAt.Value).TotalDays
                : threshold;

            var wasAutoDisabled = false;
            if (widget.InactivityAutoDisableEnabled)
            {
                widget.IsActive = false;
                wasAutoDisabled = true;
                _logger.LogWarning(
                    "InactivityMonitor: auto-disabled widget {WidgetId} '{Name}' — inactive for {Days} day(s)",
                    widget.Id, widget.Name, daysSince);
            }

            // Record audit/alert
            var notes = $"WidgetName={widget.Name};DaysSinceLastActivity={daysSince};AutoDisabled={wasAutoDisabled}";
            context.AuditLogs.Add(new AuditLog
            {
                Action = "InactivityAlert",
                EntityType = "Widget",
                EntityId = widget.Id.ToString(),
                Notes = notes,
                Timestamp = now
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
