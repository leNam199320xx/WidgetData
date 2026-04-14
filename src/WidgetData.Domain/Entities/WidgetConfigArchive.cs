namespace WidgetData.Domain.Entities;

public class WidgetConfigArchive
{
    public int Id { get; set; }
    public int WidgetId { get; set; }
    public int? ScheduleId { get; set; }
    public string? Configuration { get; set; }
    public string? ChartConfig { get; set; }
    public string? HtmlTemplate { get; set; }
    public string? Note { get; set; }
    public string ArchivedBy { get; set; } = string.Empty;
    public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;

    public Widget Widget { get; set; } = null!;
    public WidgetSchedule? Schedule { get; set; }
}
