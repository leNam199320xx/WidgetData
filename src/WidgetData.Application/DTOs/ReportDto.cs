namespace WidgetData.Application.DTOs;

public class ReportPageDto
{
    public WidgetGroupDto Page { get; set; } = new();
    public IList<ReportWidgetDto> Widgets { get; set; } = new List<ReportWidgetDto>();
}

public class ReportWidgetDto
{
    public WidgetDto? Widget { get; set; }
    public WidgetDataDto? Data { get; set; }
}

public class WidgetDataDto
{
    public IList<string>? Columns { get; set; }
    public IList<Dictionary<string, object?>>? Rows { get; set; }
    public string? Error { get; set; }
    public string? Message { get; set; }
}
