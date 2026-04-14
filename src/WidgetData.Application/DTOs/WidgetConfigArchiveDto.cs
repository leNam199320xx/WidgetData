namespace WidgetData.Application.DTOs;

public class WidgetConfigArchiveDto
{
    public int Id { get; set; }
    public int WidgetId { get; set; }
    public string? WidgetName { get; set; }
    public string? Configuration { get; set; }
    public string? ChartConfig { get; set; }
    public string? HtmlTemplate { get; set; }
    public string? Note { get; set; }
    public string TriggerSource { get; set; } = "Manual";
    public int? ScheduleId { get; set; }
    public string? ArchivedBy { get; set; }
    public DateTime ArchivedAt { get; set; }
}

public class CreateWidgetConfigArchiveDto
{
    public string? Note { get; set; }
}
