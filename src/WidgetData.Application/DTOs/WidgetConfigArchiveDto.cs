namespace WidgetData.Application.DTOs;

public class WidgetConfigArchiveDto
{
    public int Id { get; set; }
    public int WidgetId { get; set; }
    public string? WidgetName { get; set; }
    public int? ScheduleId { get; set; }
    public string? Configuration { get; set; }
    public string? ChartConfig { get; set; }
    public string? HtmlTemplate { get; set; }
    public string? Note { get; set; }
    public string ArchivedBy { get; set; } = string.Empty;
    public DateTime ArchivedAt { get; set; }
}

public class CreateWidgetConfigArchiveDto
{
    public int WidgetId { get; set; }
    public int? ScheduleId { get; set; }
    public string? Note { get; set; }
}

public class RestoreWidgetConfigArchiveDto
{
    public int ArchiveId { get; set; }
}
