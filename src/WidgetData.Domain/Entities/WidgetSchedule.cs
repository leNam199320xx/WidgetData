using WidgetData.Domain.Enums;

namespace WidgetData.Domain.Entities;

public class WidgetSchedule
{
    public int Id { get; set; }
    public int WidgetId { get; set; }
    public string CronExpression { get; set; } = string.Empty;
    public string Timezone { get; set; } = "UTC";
    public bool IsEnabled { get; set; } = true;
    public bool RetryOnFailure { get; set; } = false;
    public int MaxRetries { get; set; } = 3;
    public bool ArchiveConfigOnRun { get; set; } = false;
    public DateTime? LastRunAt { get; set; }
    public ExecutionStatus? LastRunStatus { get; set; }
    public DateTime? NextRunAt { get; set; }
    public string? HangfireJobId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Widget Widget { get; set; } = null!;
}

