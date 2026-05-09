using WidgetData.Domain.Enums;

namespace WidgetData.Domain.Entities;

/// <summary>
/// Trang của một tenant, có thể chứa nhiều widget theo thứ tự bố cục.
/// </summary>
public class Page
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    /// <summary>Slug dùng trong URL: /api/pages/{slug}</summary>
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ScreenType ScreenType { get; set; } = ScreenType.Frontend;
    public ScreenLifecycleState LifecycleState { get; set; } = ScreenLifecycleState.Draft;
    public int CurrentVersion { get; set; } = 1;
    public DateTime? PublishedAt { get; set; }
    public string? PublishedBy { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public ICollection<PageWidget> PageWidgets { get; set; } = new List<PageWidget>();
    public ICollection<PageVersion> Versions { get; set; } = new List<PageVersion>();
}
