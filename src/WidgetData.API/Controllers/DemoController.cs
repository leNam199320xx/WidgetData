using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Interfaces;

namespace WidgetData.API.Controllers;

/// <summary>
/// Endpoint công khai trả về dữ liệu demo theo template.
/// Dùng để showcase sản phẩm trước khi đăng ký.
///   GET /api/demo/sales  — dashboard cửa hàng
///   GET /api/demo/course — dashboard học tập
///   GET /api/demo/news   — dashboard tin tức
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class DemoController : ControllerBase
{
    private static readonly IReadOnlySet<string> AllowedTemplates =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "sales", "course", "news" };

    private readonly IPageService _pageService;
    private readonly ITenantRepository _tenantRepo;

    public DemoController(IPageService pageService, ITenantRepository tenantRepo)
    {
        _pageService = pageService;
        _tenantRepo = tenantRepo;
    }

    [HttpGet("{template}")]
    public async Task<IActionResult> GetDemoTemplate(string template)
    {
        if (!AllowedTemplates.Contains(template))
            return BadRequest(new { error = "Template không hợp lệ. Hãy dùng: sales, course, hoặc news." });

        var tenant = await _tenantRepo.GetBySlugAsync("demo");
        if (tenant == null)
            return NotFound(new { error = "Demo tenant chưa được khởi tạo." });

        var page = await _pageService.GetBySlugAsync(template.ToLowerInvariant(), tenant.Id);
        if (page == null)
            return NotFound(new { error = $"Trang demo '{template}' chưa có dữ liệu." });

        return Ok(page);
    }

    /// <summary>Liệt kê tất cả trang demo có sẵn.</summary>
    [HttpGet]
    public async Task<IActionResult> ListDemoPages()
    {
        var tenant = await _tenantRepo.GetBySlugAsync("demo");
        if (tenant == null)
            return NotFound(new { error = "Demo tenant chưa được khởi tạo." });

        var pages = await _pageService.GetAllAsync(tenant.Id);
        return Ok(pages);
    }
}
