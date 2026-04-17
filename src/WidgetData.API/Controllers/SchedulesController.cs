using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;

namespace WidgetData.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SchedulesController : ControllerBase
{
    private readonly IScheduleService _service;

    public SchedulesController(IScheduleService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateScheduleDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateScheduleDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        return result ? NoContent() : NotFound();
    }

    [HttpPost("{id}/enable")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Enable(int id)
    {
        var result = await _service.EnableAsync(id);
        return result ? Ok() : NotFound();
    }

    [HttpPost("{id}/disable")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Disable(int id)
    {
        var result = await _service.DisableAsync(id);
        return result ? Ok() : NotFound();
    }

    [HttpPost("{id}/trigger")]
    public async Task<IActionResult> Trigger(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "schedule";
        var result = await _service.TriggerAsync(id, userId);
        return result == null ? NotFound() : Ok(result);
    }
}
