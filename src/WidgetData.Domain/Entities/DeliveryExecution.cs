using WidgetData.Domain.Enums;

namespace WidgetData.Domain.Entities;

public class DeliveryExecution
{
    public int Id { get; set; }
    public int DeliveryTargetId { get; set; }
    public ExecutionStatus Status { get; set; }
    public string? Message { get; set; }
    public string? TriggeredBy { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    public DeliveryTarget DeliveryTarget { get; set; } = null!;
}
