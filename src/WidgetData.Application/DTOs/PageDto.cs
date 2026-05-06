using System.ComponentModel.DataAnnotations;

namespace WidgetData.Application.DTOs;

public class PageDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public IList<PageWidgetDto> Widgets { get; set; } = new List<PageWidgetDto>();
}

public class PageWidgetDto
{
    public int Id { get; set; }
    public int WidgetId { get; set; }
    public string WidgetName { get; set; } = string.Empty;
    public string? FriendlyLabel { get; set; }
    public string? HtmlTemplate { get; set; }
    public string? Configuration { get; set; }
    public string WidgetType { get; set; } = string.Empty;
    public int Position { get; set; }
    public int Width { get; set; }
}

public class CreatePageDto
{
    [Required]
    [StringLength(300, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 1)]
    [RegularExpression(@"^[a-z0-9\-]+$", ErrorMessage = "Slug chỉ được chứa chữ thường, số và dấu gạch ngang.")]
    public string Slug { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }
}

public class UpdatePageDto : CreatePageDto
{
    public bool IsActive { get; set; } = true;
}

public class PageWidgetLayoutDto
{
    [Range(1, int.MaxValue)]
    public int WidgetId { get; set; }
    [Range(0, 1000)]
    public int Position { get; set; }
    [Range(1, 12)]
    public int Width { get; set; } = 6;
}
