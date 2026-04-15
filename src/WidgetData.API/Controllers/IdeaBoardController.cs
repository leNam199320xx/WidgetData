using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;

namespace WidgetData.API.Controllers;

[ApiController]
[Route("api/idea-board")]
[Authorize]
public class IdeaBoardController : ControllerBase
{
    private readonly IIdeaBoardService _service;

    public IdeaBoardController(IIdeaBoardService service)
    {
        _service = service;
    }

    // ── Posts ─────────────────────────────────────────────────────────────

    [HttpPost("posts")]
    public async Task<IActionResult> CreatePost([FromBody] CreateIdeaPostDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var result = await _service.CreatePostAsync(dto, userId);
        return CreatedAtAction(nameof(GetPost), new { id = result.Id }, result);
    }

    [HttpGet("posts/{id}")]
    public async Task<IActionResult> GetPost(int id)
    {
        var result = await _service.GetPostByIdAsync(id);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("widgets/{widgetId}/posts")]
    public async Task<IActionResult> GetPostsByWidget(int widgetId)
        => Ok(await _service.GetPostsByWidgetIdAsync(widgetId));

    // ── Results ───────────────────────────────────────────────────────────

    [HttpGet("posts/{id}/results")]
    public async Task<IActionResult> GetResults(int id)
        => Ok(await _service.GetResultsByPostIdAsync(id));

    /// <summary>External subscribers POST their results here. Requires authentication.</summary>
    [HttpPost("posts/{id}/results")]
    public async Task<IActionResult> SubmitResult(int id, [FromQuery] int subscriptionId, [FromBody] CreateIdeaResultDto dto)
    {
        var result = await _service.SubmitResultAsync(id, subscriptionId, dto);
        return result == null ? NotFound() : Ok(result);
    }

    // ── Subscriptions ─────────────────────────────────────────────────────

    [HttpGet("widgets/{widgetId}/subscriptions")]
    public async Task<IActionResult> GetSubscriptions(int widgetId)
        => Ok(await _service.GetSubscriptionsByWidgetIdAsync(widgetId));

    [HttpPost("subscriptions")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateIdeaSubscriptionDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var result = await _service.CreateSubscriptionAsync(dto, userId);
        return Ok(result);
    }

    [HttpPut("subscriptions/{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateSubscription(int id, [FromBody] UpdateIdeaSubscriptionDto dto)
    {
        var result = await _service.UpdateSubscriptionAsync(id, dto);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("subscriptions/{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteSubscription(int id)
    {
        var result = await _service.DeleteSubscriptionAsync(id);
        return result ? NoContent() : NotFound();
    }
}
