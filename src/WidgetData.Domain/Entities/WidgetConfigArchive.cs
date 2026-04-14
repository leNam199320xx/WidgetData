namespace WidgetData.Domain.Entities;

public class WidgetConfigArchive
{
    public int Id { get; set; }
    public int WidgetId { get; set; }
    public string? Configuration { get; set; }
    public string? ChartConfig { get; set; }
    public string? HtmlTemplate { get; set; }
    public string? Note { get; set; }
    /// <summary>OnSave, Manual, Schedule</summary>
    public string TriggerSource { get; set; } = "Manual";
    public int? ScheduleId { get; set; }
    public string? ArchivedBy { get; set; }
    public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;

    public Widget Widget { get; set; } = null!;
}
