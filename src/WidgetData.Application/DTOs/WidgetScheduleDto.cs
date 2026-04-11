using WidgetData.Domain.Enums;

namespace WidgetData.Application.DTOs;

public class WidgetScheduleDto
{
    public int Id { get; set; }
    public int WidgetId { get; set; }
    public string? WidgetName { get; set; }
    public string CronExpression { get; set; } = string.Empty;
    public string Timezone { get; set; } = "UTC";
    public bool IsEnabled { get; set; }
    public bool RetryOnFailure { get; set; }
    public int MaxRetries { get; set; }
    public DateTime? LastRunAt { get; set; }
    public ExecutionStatus? LastRunStatus { get; set; }
    public DateTime? NextRunAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateScheduleDto
{
    public int WidgetId { get; set; }
    public string CronExpression { get; set; } = string.Empty;
    public string Timezone { get; set; } = "UTC";
    public bool IsEnabled { get; set; } = true;
    public bool RetryOnFailure { get; set; } = false;
    public int MaxRetries { get; set; } = 3;
}

public class UpdateScheduleDto : CreateScheduleDto { }
