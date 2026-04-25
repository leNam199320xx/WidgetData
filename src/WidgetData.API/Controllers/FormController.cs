using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;

namespace WidgetData.API.Controllers;

/// <summary>
/// API công khai dành cho Form widget:
///   GET  /api/form/{widgetId}/schema       — lấy định nghĩa form (public)
///   POST /api/form/{widgetId}              — submit dữ liệu (public)
///   GET  /api/form/{widgetId}/submissions  — xem các submissions (Admin/Manager)
///   DELETE /api/form/submissions/{id}     — xóa submission (Admin)
/// </summary>
[ApiController]
[Route("api/form")]
public class FormController : ControllerBase
{
    private readonly IFormService _formService;

    public FormController(IFormService formService)
    {
        _formService = formService;
    }

    // ── Public: lấy schema form ────────────────────────────────────────────

    [HttpGet("{widgetId}/schema")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSchema(int widgetId)
    {
        var schema = await _formService.GetSchemaAsync(widgetId);
        return schema == null ? NotFound() : Ok(schema);
    }

    // ── Public: submit form ────────────────────────────────────────────────

    [HttpPost("{widgetId}")]
    [AllowAnonymous]
    public async Task<IActionResult> Submit(int widgetId, [FromBody] Dictionary<string, string?> data)
    {
        if (data == null)
            return BadRequest(new { error = "Dữ liệu không hợp lệ." });

        var submittedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        try
        {
            var dto = new CreateFormSubmissionDto { WidgetId = widgetId, Data = data };
            var result = await _formService.SubmitAsync(dto, submittedBy, ip);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Form không tồn tại." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ── Admin: xem submissions ─────────────────────────────────────────────

    [HttpGet("{widgetId}/submissions")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetSubmissions(int widgetId)
        => Ok(await _formService.GetSubmissionsAsync(widgetId));

    // ── Admin: xóa submission ──────────────────────────────────────────────

    [HttpDelete("submissions/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSubmission(int id)
    {
        var result = await _formService.DeleteSubmissionAsync(id);
        return result ? NoContent() : NotFound();
    }
}
