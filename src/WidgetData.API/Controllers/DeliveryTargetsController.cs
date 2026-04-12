using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;

namespace WidgetData.API.Controllers;

[ApiController]
[Route("api/delivery-targets")]
[Authorize]
public class DeliveryTargetsController : ControllerBase
{
    private readonly IDeliveryService _service;

    public DeliveryTargetsController(IDeliveryService service)
    {
        _service = service;
    }

    [HttpGet("widget/{widgetId}")]
    public async Task<IActionResult> GetByWidget(int widgetId)
        => Ok(await _service.GetTargetsAsync(widgetId));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetTargetByIdAsync(id);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateDeliveryTargetDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var result = await _service.CreateTargetAsync(dto, userId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDeliveryTargetDto dto)
    {
        var result = await _service.UpdateTargetAsync(id, dto);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteTargetAsync(id);
        return result ? NoContent() : NotFound();
    }
}
