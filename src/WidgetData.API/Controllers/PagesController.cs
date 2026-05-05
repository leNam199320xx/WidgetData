using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Interfaces;

namespace WidgetData.API.Controllers;

/// <summary>
/// Quản lý các trang (Page) của tenant.
///   GET  /api/pages/public/{slug}       – public, không cần auth (cho embed)
///   GET  /api/pages                     – lấy danh sách trang của tenant hiện tại
///   POST /api/pages                     – tạo trang mới
///   PUT  /api/pages/{id}                – cập nhật trang
///   DELETE /api/pages/{id}              – xóa trang
///   POST /api/pages/{id}/widgets        – thêm widget vào trang
///   DELETE /api/pages/{id}/widgets/{wid} – xóa widget khỏi trang
///   PUT  /api/pages/{id}/widgets/{wid}  – cập nhật layout widget
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PagesController : ControllerBase
{
    private readonly IPageService _pageService;
    private readonly ITenantContext _tenantContext;

    public PagesController(IPageService pageService, ITenantContext tenantContext)
    {
        _pageService = pageService;
        _tenantContext = tenantContext;
    }

    // ── Public endpoint (không cần auth) ──────────────────────────────────

    [HttpGet("public/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicPage(string slug, [FromQuery] int? tenantId = null)
    {
        var page = await _pageService.GetBySlugAsync(slug, tenantId);
        return page == null ? NotFound() : Ok(page);
    }

    // ── Tenant-scoped endpoints ────────────────────────────────────────────

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll()
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Forbid();

        var pages = await _pageService.GetAllAsync(tenantId.Value);
        return Ok(pages);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id)
    {
        var page = await _pageService.GetByIdAsync(id);
        if (page == null) return NotFound();

        // Kiểm tra quyền: chỉ xem trang của tenant mình (SuperAdmin xem tất)
        if (!_tenantContext.IsSuperAdmin && _tenantContext.CurrentTenantId.HasValue
            && page.TenantId != _tenantContext.CurrentTenantId.Value)
            return Forbid();

        return Ok(page);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] CreatePageDto dto)
    {
        var tenantId = GetCurrentTenantId();
        if (tenantId == null) return Forbid();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var result = await _pageService.CreateAsync(dto, tenantId.Value, userId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Admin,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePageDto dto)
    {
        var page = await _pageService.GetByIdAsync(id);
        if (page == null) return NotFound();
        if (!CanManagePage(page)) return Forbid();

        var result = await _pageService.UpdateAsync(id, dto);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var page = await _pageService.GetByIdAsync(id);
        if (page == null) return NotFound();
        if (!CanManagePage(page)) return Forbid();

        var result = await _pageService.DeleteAsync(id);
        return result ? NoContent() : NotFound();
    }

    // ── Widget layout management ───────────────────────────────────────────

    [HttpPost("{id:int}/widgets")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Admin,Manager")]
    public async Task<IActionResult> AddWidget(int id, [FromBody] PageWidgetLayoutDto dto)
    {
        var page = await _pageService.GetByIdAsync(id);
        if (page == null) return NotFound();
        if (!CanManagePage(page)) return Forbid();

        await _pageService.AddWidgetAsync(id, dto.WidgetId, dto.Position, dto.Width);
        return Ok();
    }

    [HttpDelete("{id:int}/widgets/{widgetId:int}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Admin,Manager")]
    public async Task<IActionResult> RemoveWidget(int id, int widgetId)
    {
        var page = await _pageService.GetByIdAsync(id);
        if (page == null) return NotFound();
        if (!CanManagePage(page)) return Forbid();

        await _pageService.RemoveWidgetAsync(id, widgetId);
        return NoContent();
    }

    [HttpPut("{id:int}/widgets/{widgetId:int}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Admin,Manager")]
    public async Task<IActionResult> UpdateWidgetLayout(int id, int widgetId, [FromBody] PageWidgetLayoutDto dto)
    {
        var page = await _pageService.GetByIdAsync(id);
        if (page == null) return NotFound();
        if (!CanManagePage(page)) return Forbid();

        await _pageService.UpdateWidgetLayoutAsync(id, widgetId, dto.Position, dto.Width);
        return Ok();
    }

    private int? GetCurrentTenantId()
    {
        if (_tenantContext.IsSuperAdmin)
        {
            // SuperAdmin có thể chỉ định tenantId qua query param, hoặc lấy từ claim
            var tenantClaim = User.FindFirstValue("tenant_id");
            if (int.TryParse(tenantClaim, out var tid)) return tid;
            return null; // SuperAdmin không bị bắt buộc chỉ định tenant
        }
        return _tenantContext.CurrentTenantId;
    }

    private bool CanManagePage(PageDto page)
    {
        if (_tenantContext.IsSuperAdmin) return true;
        return !_tenantContext.CurrentTenantId.HasValue
            || page.TenantId == _tenantContext.CurrentTenantId.Value;
    }
}
