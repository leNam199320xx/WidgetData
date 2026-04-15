namespace WidgetData.Domain.Entities;

public class IdeaPost
{
    public int Id { get; set; }
    public int WidgetId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    /// <summary>Comma-separated label names, e.g. "billing,support"</summary>
    public string? Labels { get; set; }
    public string Status { get; set; } = "Pending";
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    public Widget Widget { get; set; } = null!;
    public ICollection<IdeaResult> Results { get; set; } = new List<IdeaResult>();
}
