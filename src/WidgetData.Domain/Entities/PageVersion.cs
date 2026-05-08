namespace WidgetData.Domain.Entities;

public class PageVersion
{
    public int Id { get; set; }
    public int PageId { get; set; }
    public int TenantId { get; set; }
    public int VersionNumber { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public string Action { get; set; } = "DraftSaved";
    public string? Note { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Page Page { get; set; } = null!;
}
