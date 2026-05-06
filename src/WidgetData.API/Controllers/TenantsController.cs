using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;

namespace WidgetData.API.Controllers;

/// <summary>
/// Quản lý tenant – chỉ dành cho SuperAdmin.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _service;

    public TenantsController(ITenantService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("admin-stats")]
    public async Task<IActionResult> GetAdminStats()
        => Ok(await _service.GetAdminStatsAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var result = await _service.GetBySlugAsync(slug);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTenantDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        return result ? NoContent() : NotFound();
    }

    // ── User management ────────────────────────────────────────────────────

    /// <summary>Liệt kê tất cả user thuộc tenant.</summary>
    [HttpGet("{id:int}/users")]
    public async Task<IActionResult> GetUsers(int id)
        => Ok(await _service.GetUsersAsync(id));

    /// <summary>Gán user vào tenant với role chỉ định (TenantAdmin hoặc TenantUser).</summary>
    [HttpPost("{id:int}/users")]
    public async Task<IActionResult> AssignUser(int id, [FromBody] AssignUserToTenantDto dto)
    {
        var result = await _service.AssignUserAsync(id, dto);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>Gỡ user khỏi tenant (xóa TenantId và role tenant).</summary>
    [HttpDelete("{id:int}/users/{userId}")]
    public async Task<IActionResult> RemoveUser(int id, string userId)
    {
        var result = await _service.RemoveUserAsync(id, userId);
        return result ? NoContent() : NotFound();
    }
}
