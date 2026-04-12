using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Infrastructure.Data;
using WidgetData.Infrastructure.Services;
using WidgetData.Tests.TestData;

namespace WidgetData.Tests.Services;

public class DashboardServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new DashboardService(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<DataSource> SeedDataSourceAsync(int id = 1, bool isActive = true)
    {
        var ds = new DataSource
        {
            Id = id,
            Name = $"DataSource {id}",
            SourceType = DataSourceType.SQLite,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
        _context.DataSources.Add(ds);
        await _context.SaveChangesAsync();
        return ds;
    }

    private async Task<Widget> SeedWidgetAsync(int id = 1, int dataSourceId = 1, bool isActive = true)
    {
        var widget = new Widget
        {
            Id = id,
            Name = $"Widget {id}",
            WidgetType = WidgetType.Chart,
            DataSourceId = dataSourceId,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
        _context.Widgets.Add(widget);
        await _context.SaveChangesAsync();
        return widget;
    }

    private async Task<WidgetSchedule> SeedScheduleAsync(int id = 1, int widgetId = 1, bool isEnabled = true)
    {
        var schedule = new WidgetSchedule
        {
            Id = id,
            WidgetId = widgetId,
            CronExpression = "0 * * * *",
            IsEnabled = isEnabled,
            CreatedAt = DateTime.UtcNow
        };
        _context.WidgetSchedules.Add(schedule);
        await _context.SaveChangesAsync();
        return schedule;
    }

    private async Task<WidgetExecution> SeedExecutionAsync(int id, int widgetId,
        ExecutionStatus status, DateTime? startedAt = null)
    {
        var execution = new WidgetExecution
        {
            Id = id,
            ExecutionId = Guid.NewGuid(),
            WidgetId = widgetId,
            Status = status,
            TriggeredBy = ExecutionTrigger.Manual,
            StartedAt = startedAt ?? DateTime.UtcNow
        };
        _context.WidgetExecutions.Add(execution);
        await _context.SaveChangesAsync();
        return execution;
    }

    // ─── GetStatsAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStatsAsync_EmptyDatabase_ReturnsZeroCounts()
    {
        var stats = await _service.GetStatsAsync();

        Assert.Equal(0, stats.TotalWidgets);
        Assert.Equal(0, stats.ActiveWidgets);
        Assert.Equal(0, stats.TotalDataSources);
        Assert.Equal(0, stats.ActiveDataSources);
        Assert.Equal(0, stats.TotalSchedules);
        Assert.Equal(0, stats.ActiveSchedules);
        Assert.Equal(0, stats.TotalExecutions);
        Assert.Equal(0, stats.SuccessfulExecutions);
        Assert.Equal(0, stats.FailedExecutions);
        Assert.Empty(stats.RecentExecutions);
    }

    [Fact]
    public async Task GetStatsAsync_CountsWidgetsCorrectly()
    {
        await SeedDataSourceAsync(1);
        await SeedWidgetAsync(1, 1, isActive: true);
        await SeedWidgetAsync(2, 1, isActive: true);
        await SeedWidgetAsync(3, 1, isActive: false);

        var stats = await _service.GetStatsAsync();

        Assert.Equal(3, stats.TotalWidgets);
        Assert.Equal(2, stats.ActiveWidgets);
    }

    [Fact]
    public async Task GetStatsAsync_CountsDataSourcesCorrectly()
    {
        await SeedDataSourceAsync(1, isActive: true);
        await SeedDataSourceAsync(2, isActive: true);
        await SeedDataSourceAsync(3, isActive: false);

        var stats = await _service.GetStatsAsync();

        Assert.Equal(3, stats.TotalDataSources);
        Assert.Equal(2, stats.ActiveDataSources);
    }

    [Fact]
    public async Task GetStatsAsync_CountsSchedulesCorrectly()
    {
        await SeedDataSourceAsync(1);
        await SeedWidgetAsync(1, 1);
        await SeedScheduleAsync(1, 1, isEnabled: true);
        await SeedScheduleAsync(2, 1, isEnabled: true);
        await SeedScheduleAsync(3, 1, isEnabled: false);

        var stats = await _service.GetStatsAsync();

        Assert.Equal(3, stats.TotalSchedules);
        Assert.Equal(2, stats.ActiveSchedules);
    }

    [Fact]
    public async Task GetStatsAsync_CountsExecutionStatusesCorrectly()
    {
        await SeedDataSourceAsync(1);
        await SeedWidgetAsync(1, 1);
        await SeedExecutionAsync(1, 1, ExecutionStatus.Success);
        await SeedExecutionAsync(2, 1, ExecutionStatus.Success);
        await SeedExecutionAsync(3, 1, ExecutionStatus.Failed);
        await SeedExecutionAsync(4, 1, ExecutionStatus.Running);

        var stats = await _service.GetStatsAsync();

        Assert.Equal(4, stats.TotalExecutions);
        Assert.Equal(2, stats.SuccessfulExecutions);
        Assert.Equal(1, stats.FailedExecutions);
    }

    [Fact]
    public async Task GetStatsAsync_RecentExecutions_OnlyLast7Days()
    {
        await SeedDataSourceAsync(1);
        await SeedWidgetAsync(1, 1);

        // Execution trong 7 ngày gần đây
        await SeedExecutionAsync(1, 1, ExecutionStatus.Success, DateTime.UtcNow.AddDays(-3));
        await SeedExecutionAsync(2, 1, ExecutionStatus.Failed, DateTime.UtcNow.AddDays(-1));

        // Execution cũ hơn 7 ngày
        await SeedExecutionAsync(3, 1, ExecutionStatus.Success, DateTime.UtcNow.AddDays(-10));

        var stats = await _service.GetStatsAsync();

        Assert.Equal(2, stats.RecentExecutions.Count);
    }

    [Fact]
    public async Task GetStatsAsync_RecentExecutions_MaxTen()
    {
        await SeedDataSourceAsync(1);
        await SeedWidgetAsync(1, 1);

        for (var i = 1; i <= 15; i++)
            await SeedExecutionAsync(i, 1, ExecutionStatus.Success, DateTime.UtcNow.AddMinutes(-i));

        var stats = await _service.GetStatsAsync();

        Assert.Equal(10, stats.RecentExecutions.Count);
    }

    [Fact]
    public async Task GetStatsAsync_RecentExecutions_OrderedByStartedAtDescending()
    {
        await SeedDataSourceAsync(1);
        await SeedWidgetAsync(1, 1);
        var older = DateTime.UtcNow.AddDays(-2);
        var newer = DateTime.UtcNow.AddDays(-1);

        await SeedExecutionAsync(1, 1, ExecutionStatus.Success, older);
        await SeedExecutionAsync(2, 1, ExecutionStatus.Failed, newer);

        var stats = await _service.GetStatsAsync();

        Assert.Equal(2, stats.RecentExecutions[0].Id);
        Assert.Equal(1, stats.RecentExecutions[1].Id);
    }

    [Fact]
    public async Task GetStatsAsync_RecentExecutions_IncludeWidgetName()
    {
        await SeedDataSourceAsync(1);
        var widget = await SeedWidgetAsync(1, 1);
        await SeedExecutionAsync(1, 1, ExecutionStatus.Success);

        var stats = await _service.GetStatsAsync();

        Assert.Equal(widget.Name, stats.RecentExecutions[0].WidgetName);
    }
}
