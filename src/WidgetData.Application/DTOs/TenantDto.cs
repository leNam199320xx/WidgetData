using System.ComponentModel.DataAnnotations;

namespace WidgetData.Application.DTOs;

public class TenantDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Plan { get; set; } = "free";
    public string? ContactEmail { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserCount { get; set; }
    public int PageCount { get; set; }
    public int WidgetCount { get; set; }
}

public class CreateTenantDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    [RegularExpression(@"^[a-z0-9\-]+$", ErrorMessage = "Slug chỉ được chứa chữ thường, số và dấu gạch ngang.")]
    public string Slug { get; set; } = string.Empty;

    [StringLength(100)]
    public string Plan { get; set; } = "free";

    [EmailAddress]
    [StringLength(256)]
    public string? ContactEmail { get; set; }
}

public class UpdateTenantDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string Plan { get; set; } = "free";

    [EmailAddress]
    [StringLength(256)]
    public string? ContactEmail { get; set; }

    public bool IsActive { get; set; } = true;
}

public class AdminStatsDto
{
    public int TotalTenants { get; set; }
    public int ActiveTenants { get; set; }
    public int TotalPages { get; set; }
    public int ActivePages { get; set; }
    public int TotalApiCalls { get; set; }
    public int TotalFormSubmissions { get; set; }
    public IList<TenantDto> Tenants { get; set; } = new List<TenantDto>();
}
