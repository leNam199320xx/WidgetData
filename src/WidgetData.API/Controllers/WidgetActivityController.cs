using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WidgetData.Application.Interfaces;

namespace WidgetData.API.Controllers;

[ApiController]
[Route("api/widget-activity")]
[Authorize(Roles = "Admin,Manager")]
public class WidgetActivityController : ControllerBase
{
    private readonly IWidgetActivityService _activityService;

    public WidgetActivityController(IWidgetActivityService activityService)
    {
        _activityService = activityService;
    }

    /// <summary>Paginated raw activity log for a widget.</summary>
    [HttpGet("{widgetId:int}")]
    public async Task<IActionResult> GetActivity(int widgetId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;
        var result = await _activityService.GetActivityAsync(widgetId, page, pageSize);
        return Ok(result);
    }

    /// <summary>Aggregated activity summary for a widget.</summary>
    [HttpGet("{widgetId:int}/summary")]
    public async Task<IActionResult> GetSummary(int widgetId)
    {
        var result = await _activityService.GetSummaryAsync(widgetId);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>List active widgets that have been inactive beyond the threshold. (Admin only)</summary>
    [HttpGet("inactive")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetInactive([FromQuery] int thresholdDays = 30)
    {
        if (thresholdDays < 1) thresholdDays = 30;
        var result = await _activityService.GetInactiveWidgetsAsync(thresholdDays);
        return Ok(result);
    }

    /// <summary>List all inactivity alert events recorded by the monitor. (Admin only)</summary>
    [HttpGet("alerts")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAlerts()
    {
        var result = await _activityService.GetInactivityAlertsAsync();
        return Ok(result);
    }
}
