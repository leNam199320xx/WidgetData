using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;

namespace WidgetData.API.Controllers;

/// <summary>
/// Flat /api/widget-config-archives endpoint — provides a global view across all widgets.
/// Per-widget CRUD is also available under /api/widgets/{id}/config-archives.
/// </summary>
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
}
