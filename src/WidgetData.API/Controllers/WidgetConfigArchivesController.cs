using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;

namespace WidgetData.API.Controllers;

[ApiController]
[Route("api/widget-config-archives")]
[Authorize]
public class WidgetConfigArchivesController : ControllerBase
{
    private readonly IWidgetConfigArchiveService _service;

    public WidgetConfigArchivesController(IWidgetConfigArchiveService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("widget/{widgetId}")]
    public async Task<IActionResult> GetByWidget(int widgetId)
        => Ok(await _service.GetByWidgetIdAsync(widgetId));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWidgetConfigArchiveDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var result = await _service.CreateAsync(dto, userId);
        return Ok(result);
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> Restore(int id)
    {
        var result = await _service.RestoreAsync(id);
        return result ? Ok() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        return result ? NoContent() : NotFound();
    }
}
