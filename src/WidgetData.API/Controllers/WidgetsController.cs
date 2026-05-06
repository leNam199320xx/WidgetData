using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;

namespace WidgetData.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WidgetsController : ControllerBase
{
    private readonly IWidgetService _service;
    private readonly IPermissionService _permissionService;
    private readonly IExportService _exportService;
    private readonly IDeliveryService _deliveryService;
    private readonly IWidgetConfigArchiveService _archiveService;

    public WidgetsController(
        IWidgetService service,
        IPermissionService permissionService,
        IExportService exportService,
        IDeliveryService deliveryService,
        IWidgetConfigArchiveService archiveService)
    {
        _service = service;
        _permissionService = permissionService;
        _exportService = exportService;
        _deliveryService = deliveryService;
        _archiveService = archiveService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

        IEnumerable<WidgetDto> source;
        if (userId != null && !roles.Contains("Admin"))
        {
            var accessibleIds = (await _permissionService.GetAccessibleWidgetIdsAsync(userId)).ToHashSet();
            var all = await _service.GetAllAsync();
            source = all.Where(w => accessibleIds.Contains(w.Id));
        }
        else
        {
            source = await _service.GetAllAsync();
        }

        var list = source.ToList();
        var total = list.Count;
        var items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Ok(new PagedResult<WidgetDto> { Items = items, Total = total, Page = page, PageSize = pageSize });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateWidgetDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var result = await _service.CreateAsync(dto, userId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateWidgetDto dto)
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

    [HttpPost("{id}/execute")]
    public async Task<IActionResult> Execute(int id, [FromQuery] int? scheduleId = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

        if (!roles.Contains("Admin"))
        {
            var hasAccess = await _permissionService.HasWidgetAccessAsync(userId, id, "execute");
            if (!hasAccess) return Forbid();
        }

        var widget = await _service.GetByIdAsync(id);
        if (widget == null) return NotFound();

        var result = await _service.ExecuteAsync(id, userId, scheduleId);
        return Ok(result);
    }

    [HttpGet("{id}/data")]
    public async Task<IActionResult> GetData(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

        if (!roles.Contains("Admin"))
        {
            var hasAccess = await _permissionService.HasWidgetAccessAsync(userId, id, "view");
            if (!hasAccess) return Forbid();
        }

        var result = await _service.GetDataAsync(id);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Endpoint công khai để nhúng widget vào website bên ngoài.
    /// Không yêu cầu xác thực – chỉ dùng cho widget được đánh dấu IsActive.
    /// </summary>
    [HttpGet("{id}/embed")]
    [AllowAnonymous]
    public async Task<IActionResult> GetEmbedData(int id)
    {
        var widget = await _service.GetByIdAsync(id);
        if (widget == null || !widget.IsActive) return NotFound();

        var result = await _service.GetDataAsync(id);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetHistory(int id)
        => Ok(await _service.GetHistoryAsync(id));

    [HttpGet("{id}/export")]
    public async Task<IActionResult> Export(int id, [FromQuery] string format = "csv")
    {
        try
        {
            var data = await _exportService.ExportAsync(id, format);
            var contentType = _exportService.GetContentType(format);
            var fileName = _exportService.GetFileName(id, format);
            return File(data, contentType, fileName);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/deliver/{deliveryTargetId}")]
    public async Task<IActionResult> Deliver(int id, int deliveryTargetId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        try
        {
            var result = await _deliveryService.DeliverAsync(id, deliveryTargetId, userId);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("{id}/deliveries")]
    public async Task<IActionResult> GetDeliveries(int id)
        => Ok(await _deliveryService.GetExecutionsAsync(id));

    // Config Archives
    [HttpGet("{id}/config-archives")]
    public async Task<IActionResult> GetConfigArchives(int id)
        => Ok(await _archiveService.GetByWidgetIdAsync(id));

    [HttpPost("{id}/config-archives")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateConfigArchive(int id, [FromBody] CreateWidgetConfigArchiveDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var result = await _archiveService.CreateAsync(id, dto, userId, "Manual");
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("{id}/config-archives/{archiveId}/restore")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RestoreConfigArchive(int id, int archiveId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var result = await _archiveService.RestoreAsync(id, archiveId, userId);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id}/config-archives/{archiveId}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteConfigArchive(int id, int archiveId)
    {
        var result = await _archiveService.DeleteAsync(archiveId);
        return result ? NoContent() : NotFound();
    }
}
