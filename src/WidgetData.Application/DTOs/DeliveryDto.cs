using WidgetData.Domain.Enums;

namespace WidgetData.Application.DTOs;

public class DeliveryTargetDto
{
    public int Id { get; set; }
    public int WidgetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DeliveryType Type { get; set; }
    public string? Configuration { get; set; }
    public bool IsEnabled { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateDeliveryTargetDto
{
    public int WidgetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DeliveryType Type { get; set; }
    public string? Configuration { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class UpdateDeliveryTargetDto : CreateDeliveryTargetDto { }

public class DeliveryExecutionDto
{
    public int Id { get; set; }
    public int DeliveryTargetId { get; set; }
    public string? DeliveryTargetName { get; set; }
    public int WidgetId { get; set; }
    public ExecutionStatus Status { get; set; }
    public string? Message { get; set; }
    public string? TriggeredBy { get; set; }
    public DateTime ExecutedAt { get; set; }
}
