using WidgetData.Domain.Enums;

namespace WidgetData.Application.DTOs;

public class WidgetDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public WidgetType WidgetType { get; set; }
    public string? Description { get; set; }
    public int DataSourceId { get; set; }
    public string? DataSourceName { get; set; }
    public string? Configuration { get; set; }
    public string? ChartConfig { get; set; }
    public bool IsActive { get; set; }
    public bool CacheEnabled { get; set; }
    public int CacheTtlMinutes { get; set; }
    public DateTime? LastExecutedAt { get; set; }
    public int? LastRowCount { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateWidgetDto
{
    public string Name { get; set; } = string.Empty;
    public WidgetType WidgetType { get; set; }
    public string? Description { get; set; }
    public int DataSourceId { get; set; }
    public string? Configuration { get; set; }
    public string? ChartConfig { get; set; }
    public bool CacheEnabled { get; set; } = false;
    public int CacheTtlMinutes { get; set; } = 15;
}

public class UpdateWidgetDto : CreateWidgetDto
{
    public bool IsActive { get; set; } = true;
}
