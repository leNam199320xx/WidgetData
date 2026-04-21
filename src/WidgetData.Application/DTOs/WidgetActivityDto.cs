namespace WidgetData.Application.DTOs;

public class WidgetActivityDto
{
    public int Id { get; set; }
    public int WidgetId { get; set; }
    public string ApiEndpoint { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTime CalledAt { get; set; }
    public long? ResponseTimeMs { get; set; }
    public int StatusCode { get; set; }
}

public class WidgetActivitySummaryDto
{
    public int WidgetId { get; set; }
    public string? WidgetName { get; set; }
    public int TotalCalls { get; set; }
    public int UniqueUsers { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public IList<EndpointCallCountDto> TopEndpoints { get; set; } = new List<EndpointCallCountDto>();
}

public class EndpointCallCountDto
{
    public string ApiEndpoint { get; set; } = string.Empty;
    public int CallCount { get; set; }
}

public class InactivityAlertDto
{
    public int WidgetId { get; set; }
    public string WidgetName { get; set; } = string.Empty;
    public int DaysSinceLastActivity { get; set; }
    public bool WasAutoDisabled { get; set; }
    public DateTime DetectedAt { get; set; }
}
