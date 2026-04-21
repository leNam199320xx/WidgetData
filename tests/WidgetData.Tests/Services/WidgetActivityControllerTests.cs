using Microsoft.AspNetCore.Mvc;
using Moq;
using WidgetData.API.Controllers;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;

namespace WidgetData.Tests.Services;

public class WidgetActivityControllerTests
{
    private readonly Mock<IWidgetActivityService> _serviceMock;
    private readonly WidgetActivityController _controller;

    public WidgetActivityControllerTests()
    {
        _serviceMock = new Mock<IWidgetActivityService>();
        _controller = new WidgetActivityController(_serviceMock.Object);
    }

    // ─── GetActivity ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetActivity_ReturnsOkWithPagedResult()
    {
        var paged = new PagedResult<WidgetActivityDto>
        {
            Total = 2,
            Page = 1,
            PageSize = 20,
            Items = new List<WidgetActivityDto>
            {
                new() { Id = 1, WidgetId = 5, ApiEndpoint = "data", StatusCode = 200 },
                new() { Id = 2, WidgetId = 5, ApiEndpoint = "execute", StatusCode = 200 }
            }
        };
        _serviceMock.Setup(s => s.GetActivityAsync(5, 1, 20)).ReturnsAsync(paged);

        var result = await _controller.GetActivity(5, 1, 20);

        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<PagedResult<WidgetActivityDto>>(ok.Value);
        Assert.Equal(2, returned.Total);
    }

    // ─── GetSummary ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSummary_ExistingWidget_ReturnsOk()
    {
        var summary = new WidgetActivitySummaryDto
        {
            WidgetId = 5,
            WidgetName = "My Widget",
            TotalCalls = 10,
            UniqueUsers = 3,
            LastActivityAt = DateTime.UtcNow.AddHours(-1)
        };
        _serviceMock.Setup(s => s.GetSummaryAsync(5)).ReturnsAsync(summary);

        var result = await _controller.GetSummary(5);

        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<WidgetActivitySummaryDto>(ok.Value);
        Assert.Equal(5, returned.WidgetId);
        Assert.Equal(10, returned.TotalCalls);
    }

    [Fact]
    public async Task GetSummary_NonExistentWidget_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.GetSummaryAsync(999)).ReturnsAsync((WidgetActivitySummaryDto?)null);

        var result = await _controller.GetSummary(999);

        Assert.IsType<NotFoundResult>(result);
    }

    // ─── GetInactive ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetInactive_ReturnsOkWithList()
    {
        var alerts = new List<InactivityAlertDto>
        {
            new() { WidgetId = 1, WidgetName = "Old Widget", DaysSinceLastActivity = 40, WasAutoDisabled = false }
        };
        _serviceMock.Setup(s => s.GetInactiveWidgetsAsync(30)).ReturnsAsync(alerts);

        var result = await _controller.GetInactive(30);

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<InactivityAlertDto>>(ok.Value);
        Assert.Single(list);
    }

    [Fact]
    public async Task GetInactive_InvalidThreshold_UsesDefault30()
    {
        _serviceMock.Setup(s => s.GetInactiveWidgetsAsync(30)).ReturnsAsync(new List<InactivityAlertDto>());

        var result = await _controller.GetInactive(0);

        _serviceMock.Verify(s => s.GetInactiveWidgetsAsync(30), Times.Once);
        Assert.IsType<OkObjectResult>(result);
    }

    // ─── GetAlerts ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAlerts_ReturnsOkWithAlerts()
    {
        var alerts = new List<InactivityAlertDto>
        {
            new() { WidgetId = 2, WidgetName = "Stale Widget", DaysSinceLastActivity = 35, WasAutoDisabled = true }
        };
        _serviceMock.Setup(s => s.GetInactivityAlertsAsync()).ReturnsAsync(alerts);

        var result = await _controller.GetAlerts();

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<InactivityAlertDto>>(ok.Value);
        Assert.Single(list);
        Assert.True(list.First().WasAutoDisabled);
    }
}
