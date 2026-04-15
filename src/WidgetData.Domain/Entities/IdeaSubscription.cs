namespace WidgetData.Domain.Entities;

public class IdeaSubscription
{
    public int Id { get; set; }
    public int WidgetId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Comma-separated labels this subscription listens to, e.g. "billing,support"</summary>
    public string? LabelFilter { get; set; }
    /// <summary>External webhook URL to POST the idea payload to. If null, results are stored internally.</summary>
    public string? WebhookUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Widget Widget { get; set; } = null!;
    public ICollection<IdeaResult> Results { get; set; } = new List<IdeaResult>();
}
