using WidgetData.Domain.Enums;

namespace WidgetData.Application.DTOs;

public class WidgetExecutionDto
{
    public int Id { get; set; }
    public Guid ExecutionId { get; set; }
    public int WidgetId { get; set; }
    public string? WidgetName { get; set; }
    public int? ScheduleId { get; set; }
    public ExecutionStatus Status { get; set; }
    public ExecutionTrigger TriggeredBy { get; set; }
    public string? UserId { get; set; }
    public int? RowCount { get; set; }
    public long? ExecutionTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ResultSummary { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
