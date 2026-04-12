using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;

namespace WidgetData.API.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize(Roles = "Admin")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _service;

    public PermissionsController(IPermissionService service)
    {
        _service = service;
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserPermissions(string userId)
        => Ok(await _service.GetUserPermissionsAsync(userId));

    [HttpGet("widget/{widgetId}")]
    public async Task<IActionResult> GetWidgetPermissions(int widgetId)
        => Ok(await _service.GetWidgetPermissionsAsync(widgetId));

    [HttpGet("group/{groupId}")]
    public async Task<IActionResult> GetGroupPermissions(int groupId)
        => Ok(await _service.GetGroupPermissionsAsync(groupId));

    [HttpPost("widget")]
    public async Task<IActionResult> AssignWidget([FromBody] AssignWidgetPermissionDto dto)
        => Ok(await _service.AssignWidgetPermissionAsync(dto));

    [HttpPost("group")]
    public async Task<IActionResult> AssignGroup([FromBody] AssignGroupPermissionDto dto)
        => Ok(await _service.AssignGroupPermissionAsync(dto));

    [HttpDelete("widget/{permissionId}")]
    public async Task<IActionResult> RemoveWidget(int permissionId)
    {
        var result = await _service.RemoveWidgetPermissionAsync(permissionId);
        return result ? NoContent() : NotFound();
    }

    [HttpDelete("group/{permissionId}")]
    public async Task<IActionResult> RemoveGroup(int permissionId)
    {
        var result = await _service.RemoveGroupPermissionAsync(permissionId);
        return result ? NoContent() : NotFound();
    }
}
