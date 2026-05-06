using Microsoft.AspNetCore.Identity;

namespace WidgetData.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    /// <summary>Tenant mà user này thuộc về. Null = SuperAdmin (không thuộc tenant nào).</summary>
    public int? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
}
