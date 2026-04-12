using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WidgetData.Application.Interfaces;

namespace WidgetData.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IWidgetGroupService _groupService;
    private readonly IWidgetService _widgetService;

    public ReportsController(IWidgetGroupService groupService, IWidgetService widgetService)
    {
        _groupService = groupService;
        _widgetService = widgetService;
    }

    /// <summary>Get all report pages (widget groups)</summary>
    [HttpGet("pages")]
    public async Task<IActionResult> GetPages()
    {
        var groups = await _groupService.GetAllAsync();
        return Ok(groups);
    }

    /// <summary>Get a report page with widget data</summary>
    [HttpGet("pages/{id}")]
    public async Task<IActionResult> GetPage(int id)
    {
        var group = await _groupService.GetByIdAsync(id);
        if (group == null) return NotFound();

        var widgetDataTasks = group.WidgetIds.Select(async widgetId =>
        {
            var widget = await _widgetService.GetByIdAsync(widgetId);
            var data = await _widgetService.GetDataAsync(widgetId);
            return new { widget, data };
        });

        var widgetResults = await Task.WhenAll(widgetDataTasks);
        return Ok(new { page = group, widgets = widgetResults });
    }

    /// <summary>Get data for a single widget</summary>
    [HttpGet("widgets/{id}/data")]
    public async Task<IActionResult> GetWidgetData(int id)
    {
        var data = await _widgetService.GetDataAsync(id);
        if (data == null) return NotFound();
        return Ok(data);
    }
}
