namespace WidgetData.Application.DTOs;

public class DashboardStatsDto
{
    public int TotalWidgets { get; set; }
    public int ActiveWidgets { get; set; }
    public int TotalDataSources { get; set; }
    public int ActiveDataSources { get; set; }
    public int TotalSchedules { get; set; }
    public int ActiveSchedules { get; set; }
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public IList<WidgetExecutionDto> RecentExecutions { get; set; } = new List<WidgetExecutionDto>();
}
