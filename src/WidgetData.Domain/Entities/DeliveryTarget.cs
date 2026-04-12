using WidgetData.Domain.Enums;

namespace WidgetData.Domain.Entities;

public class DeliveryTarget
{
    public int Id { get; set; }
    public int WidgetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DeliveryType Type { get; set; }
    public string? Configuration { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Widget Widget { get; set; } = null!;
    public ICollection<DeliveryExecution> Executions { get; set; } = new List<DeliveryExecution>();
}
