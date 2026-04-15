namespace WidgetData.Domain.Entities;

public class IdeaResult
{
    public int Id { get; set; }
    public int IdeaPostId { get; set; }
    public int IdeaSubscriptionId { get; set; }
    public string? ResultContent { get; set; }
    public string Status { get; set; } = "Received";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public IdeaPost IdeaPost { get; set; } = null!;
    public IdeaSubscription IdeaSubscription { get; set; } = null!;
}
