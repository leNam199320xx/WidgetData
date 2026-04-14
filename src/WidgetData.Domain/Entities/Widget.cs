using WidgetData.Domain.Enums;

namespace WidgetData.Domain.Entities;

public class Widget
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public WidgetType WidgetType { get; set; }
    public string? Description { get; set; }
    public int DataSourceId { get; set; }
    public string? Configuration { get; set; }
    public string? ChartConfig { get; set; }
    public bool IsActive { get; set; } = true;
    public bool CacheEnabled { get; set; } = false;
    public int CacheTtlMinutes { get; set; } = 15;
    public DateTime? LastExecutedAt { get; set; }
    public int? LastRowCount { get; set; }
    public string? FriendlyLabel { get; set; }
    public string? HelpText { get; set; }
    public string? HtmlTemplate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public DataSource DataSource { get; set; } = null!;
    public ICollection<WidgetGroupMember> GroupMembers { get; set; } = new List<WidgetGroupMember>();
    public ICollection<UserWidgetPermission> Permissions { get; set; } = new List<UserWidgetPermission>();
    public ICollection<DeliveryTarget> DeliveryTargets { get; set; } = new List<DeliveryTarget>();
    public ICollection<WidgetSchedule> Schedules { get; set; } = new List<WidgetSchedule>();
    public ICollection<WidgetExecution> Executions { get; set; } = new List<WidgetExecution>();
}
