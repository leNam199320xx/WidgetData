namespace WidgetData.Application.DTOs;

public class IdeaPostDto
{
    public int Id { get; set; }
    public int WidgetId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? Labels { get; set; }
    public string Status { get; set; } = "Pending";
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public IList<IdeaResultDto> Results { get; set; } = new List<IdeaResultDto>();
}

public class CreateIdeaPostDto
{
    public int WidgetId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    /// <summary>Comma-separated label names</summary>
    public string? Labels { get; set; }
}

public class IdeaSubscriptionDto
{
    public int Id { get; set; }
    public int WidgetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LabelFilter { get; set; }
    public string? WebhookUrl { get; set; }
    public bool IsActive { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateIdeaSubscriptionDto
{
    public int WidgetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LabelFilter { get; set; }
    public string? WebhookUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateIdeaSubscriptionDto
{
    public string Name { get; set; } = string.Empty;
    public string? LabelFilter { get; set; }
    public string? WebhookUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

public class IdeaResultDto
{
    public int Id { get; set; }
    public int IdeaPostId { get; set; }
    public int IdeaSubscriptionId { get; set; }
    public string? SubscriptionName { get; set; }
    public string? ResultContent { get; set; }
    public string Status { get; set; } = "Received";
    public DateTime CreatedAt { get; set; }
}

public class CreateIdeaResultDto
{
    public string? ResultContent { get; set; }
    public string Status { get; set; } = "Received";
}
