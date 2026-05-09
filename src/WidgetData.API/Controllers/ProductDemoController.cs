using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WidgetData.API.Controllers;

[ApiController]
[Route("api/product-demo")]
[AllowAnonymous]
public class ProductDemoController : ControllerBase
{
    private static readonly ConcurrentQueue<LeadSubmission> Leads = new();

    [HttpGet]
    public IActionResult GetLandingData()
    {
        return Ok(new
        {
            product = new
            {
                name = "WidgetData Insight",
                tagline = "Nền tảng giới thiệu sản phẩm + dữ liệu bán hàng theo thời gian thực",
                description = "Tạo landing page, hiển thị số liệu trực tiếp, và thu lead khách hàng trong một giải pháp duy nhất.",
                cta = "Đăng ký demo"
            },
            metrics = new[]
            {
                new { label = "Doanh nghiệp đang dùng", value = "2,500+" },
                new { label = "Tăng chuyển đổi", value = "+38%" },
                new { label = "Thời gian triển khai", value = "< 1 ngày" }
            },
            features = new[]
            {
                new { title = "Landing page động", description = "Trang giới thiệu sản phẩm tối ưu SEO, responsive trên mọi thiết bị." },
                new { title = "Dữ liệu real-time", description = "Kết nối API/DB và hiển thị biểu đồ, bảng, KPI theo thời gian thực." },
                new { title = "Lead management", description = "Form đăng ký demo gửi về backend ngay lập tức để đội sales xử lý." },
                new { title = "Triển khai nhanh", description = "Không cần build phức tạp, chạy trực tiếp từ ASP.NET Core static files." }
            },
            plans = new[]
            {
                new { name = "Starter", price = "299.000đ/tháng", description = "Cho startup cần landing + form lead cơ bản." },
                new { name = "Growth", price = "899.000đ/tháng", description = "Cho team marketing cần dashboard và tracking nâng cao." },
                new { name = "Enterprise", price = "Liên hệ", description = "Giải pháp tùy chỉnh SLA, SSO và triển khai private." }
            },
            testimonials = new[]
            {
                new { quote = "Sau 2 tuần dùng demo này, số lead chất lượng tăng rõ rệt.", author = "Linh Tran - CMO, Nova Retail" },
                new { quote = "Team kỹ thuật triển khai rất nhanh, backend API rất ổn định.", author = "Minh Hoang - CTO, FinEdge" }
            }
        });
    }

    [HttpPost("leads")]
    public IActionResult SubmitLead([FromBody] LeadRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var lead = new LeadSubmission
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Company = request.Company?.Trim(),
            Message = request.Message?.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        Leads.Enqueue(lead);

        return Ok(new
        {
            success = true,
            message = "Đăng ký demo thành công. Chúng tôi sẽ liên hệ sớm.",
            leadId = lead.Id,
            createdAtUtc = lead.CreatedAtUtc
        });
    }

    [HttpGet("leads/summary")]
    public IActionResult GetLeadSummary()
    {
        var allLeads = Leads.ToArray();
        return Ok(new
        {
            totalLeads = allLeads.Length,
            latestLeadAtUtc = allLeads.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault()?.CreatedAtUtc
        });
    }

    public sealed class LeadRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Company { get; set; }

        [StringLength(1000)]
        public string? Message { get; set; }
    }

    private sealed class LeadSubmission
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string? Company { get; init; }
        public string? Message { get; init; }
        public DateTime CreatedAtUtc { get; init; }
    }
}
