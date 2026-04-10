using WidgetData.Domain.Enums;

namespace WidgetData.Domain.Entities;

public class WidgetExecution
{
    public int Id { get; set; }
    public Guid ExecutionId { get; set; } = Guid.NewGuid();
    public int WidgetId { get; set; }
    public int? ScheduleId { get; set; }
    public ExecutionStatus Status { get; set; } = ExecutionStatus.Running;
    public ExecutionTrigger TriggeredBy { get; set; }
    public string? UserId { get; set; }
    public string? Parameters { get; set; }
    public int? RowCount { get; set; }
    public long? ExecutionTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ResultSummary { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public Widget Widget { get; set; } = null!;
}
