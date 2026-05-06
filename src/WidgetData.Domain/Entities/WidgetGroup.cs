namespace WidgetData.Domain.Entities;

public class WidgetGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int? TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public ICollection<WidgetGroupMember> Members { get; set; } = new List<WidgetGroupMember>();
    public ICollection<UserGroupPermission> Permissions { get; set; } = new List<UserGroupPermission>();
}
