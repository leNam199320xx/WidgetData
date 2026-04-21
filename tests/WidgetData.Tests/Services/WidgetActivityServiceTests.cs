using Microsoft.EntityFrameworkCore;
using Moq;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data;
using WidgetData.Infrastructure.Repositories;
using WidgetData.Infrastructure.Services;
using WidgetData.Tests.TestData;

namespace WidgetData.Tests.Services;

public class WidgetActivityServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly WidgetActivityRepository _activityRepo;
    private readonly Mock<IWidgetRepository> _widgetRepoMock;
    private readonly WidgetActivityService _service;

    public WidgetActivityServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _activityRepo = new WidgetActivityRepository(_context);
        _widgetRepoMock = new Mock<IWidgetRepository>();
        _service = new WidgetActivityService(_activityRepo, _widgetRepoMock.Object, _context);
    }

    // ─── RecordAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RecordAsync_SavesActivityToDatabase()
    {
        // Arrange: seed a widget so the FK constraint is satisfied
        var widget = TestDataBuilder.CreateWidget(1);
        widget.DataSource = null!;
        _context.Widgets.Add(widget);
        await _context.SaveChangesAsync();

        // Act
        await _service.RecordAsync(1, "data", "user1", 120, 200);

        // Assert
        var saved = await _context.WidgetApiActivities.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal(1, saved.WidgetId);
        Assert.Equal("data", saved.ApiEndpoint);
        Assert.Equal("user1", saved.UserId);
        Assert.Equal(120, saved.ResponseTimeMs);
        Assert.Equal(200, saved.StatusCode);
    }

    [Fact]
    public async Task RecordAsync_UpdatesLastActivityAtOnWidget()
    {
        var widget = TestDataBuilder.CreateWidget(1);
        widget.DataSource = null!;
        widget.LastActivityAt = null;
        _context.Widgets.Add(widget);
        await _context.SaveChangesAsync();

        await _service.RecordAsync(1, "execute", "user1", 50, 200);

        var updated = await _context.Widgets.FindAsync(1);
        Assert.NotNull(updated!.LastActivityAt);
    }

    // ─── GetActivityAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetActivityAsync_ReturnsPagedActivities()
    {
        var widget = TestDataBuilder.CreateWidget(1);
        widget.DataSource = null!;
        _context.Widgets.Add(widget);
        await _context.SaveChangesAsync();

        for (int i = 0; i < 5; i++)
        {
            _context.WidgetApiActivities.Add(new WidgetApiActivity
            {
                WidgetId = 1,
                ApiEndpoint = "data",
                UserId = $"user{i}",
                CalledAt = DateTime.UtcNow.AddMinutes(-i),
                StatusCode = 200
            });
        }
        await _context.SaveChangesAsync();

        var result = await _service.GetActivityAsync(1, page: 1, pageSize: 3);

        Assert.Equal(5, result.Total);
        Assert.Equal(3, result.Items.Count());
        Assert.Equal(1, result.Page);
        Assert.Equal(3, result.PageSize);
    }

    [Fact]
    public async Task GetActivityAsync_EmptyWidget_ReturnsEmptyPage()
    {
        var result = await _service.GetActivityAsync(999, 1, 20);

        Assert.Equal(0, result.Total);
        Assert.Empty(result.Items);
    }

    // ─── GetSummaryAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetSummaryAsync_NonExistentWidget_ReturnsNull()
    {
        _widgetRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Widget?)null);

        var result = await _service.GetSummaryAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSummaryAsync_ExistingWidget_ReturnsSummary()
    {
        var widget = TestDataBuilder.CreateWidget(2);
        widget.DataSource = null!;
        widget.LastActivityAt = DateTime.UtcNow.AddHours(-2);
        _context.Widgets.Add(widget);
        _context.WidgetApiActivities.AddRange(
            new WidgetApiActivity { WidgetId = 2, ApiEndpoint = "data", UserId = "u1", CalledAt = DateTime.UtcNow, StatusCode = 200 },
            new WidgetApiActivity { WidgetId = 2, ApiEndpoint = "data", UserId = "u2", CalledAt = DateTime.UtcNow, StatusCode = 200 },
            new WidgetApiActivity { WidgetId = 2, ApiEndpoint = "execute", UserId = "u1", CalledAt = DateTime.UtcNow, StatusCode = 200 }
        );
        await _context.SaveChangesAsync();

        _widgetRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(widget);

        var result = await _service.GetSummaryAsync(2);

        Assert.NotNull(result);
        Assert.Equal(2, result.WidgetId);
        Assert.Equal(3, result.TotalCalls);
        Assert.Equal(2, result.UniqueUsers);
        Assert.NotNull(result.LastActivityAt);
        Assert.Contains(result.TopEndpoints, e => e.ApiEndpoint == "data" && e.CallCount == 2);
    }

    // ─── GetInactiveWidgetsAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetInactiveWidgetsAsync_ReturnsWidgetsBeyondThreshold()
    {
        var oldWidget = TestDataBuilder.CreateWidget(10);
        oldWidget.DataSource = null!;
        oldWidget.LastActivityAt = DateTime.UtcNow.AddDays(-40);
        oldWidget.IsActive = true;

        var recentWidget = TestDataBuilder.CreateWidget(11);
        recentWidget.DataSource = null!;
        recentWidget.LastActivityAt = DateTime.UtcNow.AddDays(-5);
        recentWidget.IsActive = true;

        _context.Widgets.AddRange(oldWidget, recentWidget);
        await _context.SaveChangesAsync();

        var result = (await _service.GetInactiveWidgetsAsync(30)).ToList();

        Assert.Single(result);
        Assert.Equal(10, result[0].WidgetId);
    }

    [Fact]
    public async Task GetInactiveWidgetsAsync_ExcludesInactiveWidgets()
    {
        var inactiveWidget = TestDataBuilder.CreateWidget(20);
        inactiveWidget.DataSource = null!;
        inactiveWidget.LastActivityAt = DateTime.UtcNow.AddDays(-50);
        inactiveWidget.IsActive = false;
        _context.Widgets.Add(inactiveWidget);
        await _context.SaveChangesAsync();

        var result = (await _service.GetInactiveWidgetsAsync(30)).ToList();

        Assert.Empty(result);
    }
}
