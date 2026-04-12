namespace WidgetData.Domain.Entities;

public class WidgetGroupMember
{
    public int WidgetGroupId { get; set; }
    public int WidgetId { get; set; }

    public WidgetGroup WidgetGroup { get; set; } = null!;
    public Widget Widget { get; set; } = null!;
}
