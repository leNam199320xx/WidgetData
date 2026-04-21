using System.ComponentModel.DataAnnotations;
using WidgetData.Domain.Enums;

namespace WidgetData.Application.DTOs;

public class WidgetDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? FriendlyLabel { get; set; }
    public string? HelpText { get; set; }
    public WidgetType WidgetType { get; set; }
    public string? Description { get; set; }
    public int DataSourceId { get; set; }
    public string? DataSourceName { get; set; }
    public string? Configuration { get; set; }
    public string? ChartConfig { get; set; }
    public string? HtmlTemplate { get; set; }
    public bool IsActive { get; set; }
    public bool CacheEnabled { get; set; }
    public int CacheTtlMinutes { get; set; }
    public DateTime? LastExecutedAt { get; set; }
    public int? LastRowCount { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public bool InactivityAutoDisableEnabled { get; set; }
    public int InactivityThresholdDays { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public IList<int> GroupIds { get; set; } = new List<int>();
}

public class CreateWidgetDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? FriendlyLabel { get; set; }

    [StringLength(500)]
    public string? HelpText { get; set; }

    public WidgetType WidgetType { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "DataSourceId must be a valid data source.")]
    public int DataSourceId { get; set; }

    public string? Configuration { get; set; }
    public string? ChartConfig { get; set; }
    public string? HtmlTemplate { get; set; }
    public bool CacheEnabled { get; set; } = false;

    [Range(1, 1440)]
    public int CacheTtlMinutes { get; set; } = 15;

    public bool InactivityAutoDisableEnabled { get; set; } = false;

    [Range(1, 3650)]
    public int InactivityThresholdDays { get; set; } = 30;

    public IList<int> GroupIds { get; set; } = new List<int>();
}

public class UpdateWidgetDto : CreateWidgetDto
{
    public bool IsActive { get; set; } = true;
}
