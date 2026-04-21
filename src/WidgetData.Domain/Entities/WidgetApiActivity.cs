namespace WidgetData.Domain.Entities;

public class WidgetApiActivity
{
    public int Id { get; set; }
    public int WidgetId { get; set; }
    public string ApiEndpoint { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTime CalledAt { get; set; } = DateTime.UtcNow;
    public long? ResponseTimeMs { get; set; }
    public int StatusCode { get; set; }

    public Widget Widget { get; set; } = null!;
}
