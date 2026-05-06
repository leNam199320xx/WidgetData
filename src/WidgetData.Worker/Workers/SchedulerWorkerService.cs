using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Helpers;
using WidgetData.Domain.Enums;

namespace WidgetData.Worker.Workers;

/// <summary>
/// Background service chạy các WidgetSchedule đến hạn theo cron expression.
/// Cứ mỗi <c>SchedulerWorker:PollingIntervalSeconds</c> giây (mặc định 30s),
/// service query các schedule có NextRunAt &lt;= now, thực thi widget, cập nhật
/// LastRunAt / LastRunStatus / NextRunAt, và xử lý retry nếu cần.
/// </summary>
public class SchedulerWorkerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SchedulerWorkerService> _logger;
    private readonly TimeSpan _pollingInterval;
    private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(5);

    // Ngăn double-execution khi một schedule đang được chạy
    private readonly HashSet<int> _runningSchedules = new();

    public SchedulerWorkerService(
        IServiceScopeFactory scopeFactory,
        ILogger<SchedulerWorkerService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var seconds = configuration.GetValue<int>("SchedulerWorker:PollingIntervalSeconds", 30);
        _pollingInterval = TimeSpan.FromSeconds(seconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SchedulerWorkerService started. Polling every {Interval}s", _pollingInterval.TotalSeconds);

        // Ngắn một chút khi khởi động để DB/seeder có thời gian hoàn thành
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "SchedulerWorker: unhandled error during poll");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("SchedulerWorkerService stopped");
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var scheduleRepo = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();

        var now = DateTime.UtcNow;
        var dueSchedules = (await scheduleRepo.GetDueAsync(now)).ToList();

        if (dueSchedules.Count == 0)
        {
            _logger.LogDebug("SchedulerWorker: no due schedules at {Now:o}", now);
            return;
        }

        _logger.LogInformation("SchedulerWorker: {Count} schedule(s) due at {Now:o}", dueSchedules.Count, now);

        // Chạy song song các schedule, mỗi schedule trong scope riêng
        var tasks = dueSchedules
            .Where(s => !_runningSchedules.Contains(s.Id))
            .Select(s => RunScheduleAsync(s.Id, cancellationToken));

        await Task.WhenAll(tasks);
    }

    private async Task RunScheduleAsync(int scheduleId, CancellationToken cancellationToken)
    {
        lock (_runningSchedules)
        {
            if (!_runningSchedules.Add(scheduleId))
                return; // Đang chạy, bỏ qua
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var scheduleRepo = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
            var widgetService = scope.ServiceProvider.GetRequiredService<IWidgetService>();

            var schedule = await scheduleRepo.GetByIdAsync(scheduleId);
            if (schedule == null || !schedule.IsEnabled)
                return;

            _logger.LogInformation(
                "SchedulerWorker: executing schedule {ScheduleId} (widget {WidgetId}, cron '{Cron}')",
                schedule.Id, schedule.WidgetId, schedule.CronExpression);

            ExecutionStatus status;
            int attempt = 0;
            int maxAttempts = (schedule.RetryOnFailure && schedule.MaxRetries > 0)
                ? schedule.MaxRetries + 1
                : 1;

            while (true)
            {
                attempt++;
                try
                {
                    await widgetService.ExecuteAsync(schedule.WidgetId, "scheduler", schedule.Id);
                    status = ExecutionStatus.Success;
                    _logger.LogInformation(
                        "SchedulerWorker: schedule {ScheduleId} completed successfully (attempt {Attempt})",
                        schedule.Id, attempt);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "SchedulerWorker: schedule {ScheduleId} attempt {Attempt}/{Max} failed",
                        schedule.Id, attempt, maxAttempts);

                    if (attempt >= maxAttempts)
                    {
                        status = ExecutionStatus.Failed;
                        break;
                    }

                    // Chờ trước khi retry
                    await Task.Delay(_retryDelay, cancellationToken);
                }
            }

            // Cập nhật trạng thái và tính NextRunAt tiếp theo
            var now = DateTime.UtcNow;
            schedule.LastRunAt = now;
            schedule.LastRunStatus = status;
            schedule.NextRunAt = CronUtils.GetNextOccurrence(schedule.CronExpression, schedule.Timezone, now);
            schedule.UpdatedAt = now;

            await scheduleRepo.UpdateAsync(schedule);

            _logger.LogInformation(
                "SchedulerWorker: schedule {ScheduleId} → status={Status}, nextRun={NextRun:o}",
                schedule.Id, status, schedule.NextRunAt);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "SchedulerWorker: unexpected error for schedule {ScheduleId}", scheduleId);
        }
        finally
        {
            lock (_runningSchedules)
            {
                _runningSchedules.Remove(scheduleId);
            }
        }
    }
}
