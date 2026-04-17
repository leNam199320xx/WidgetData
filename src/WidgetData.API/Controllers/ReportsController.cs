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
    private readonly IPageHtmlService _pageHtmlService;

    public ReportsController(
        IWidgetGroupService groupService,
        IWidgetService widgetService,
        IPageHtmlService pageHtmlService)
    {
        _groupService = groupService;
        _widgetService = widgetService;
        _pageHtmlService = pageHtmlService;
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

    /// <summary>
    /// Build and return a complete HTML page for the given page ID.
    /// Useful for landing pages, product pages, and sales pages that can be
    /// served directly without the WidgetEngine JS client.
    /// </summary>
    /// <param name="id">Page (WidgetGroup) ID.</param>
    /// <param name="standalone">
    ///   <c>true</c> (default) – full &lt;!DOCTYPE html&gt; document with embedded CSS.<br/>
    ///   <c>false</c> – inner grid fragment only (embed into an existing page).
    /// </param>
    /// <param name="cssUrl">
    ///   Optional URL to an external widget-engine.css file.
    ///   If omitted, minimal CSS is embedded inline.
    /// </param>
    [HttpGet("pages/{id}/html")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPageHtml(
        int id,
        [FromQuery] bool standalone = true,
        [FromQuery] string? cssUrl = null)
    {
        try
        {
            var html = await _pageHtmlService.BuildAsync(id, standalone, cssUrl);
            return Content(html, "text/html; charset=utf-8");
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
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
