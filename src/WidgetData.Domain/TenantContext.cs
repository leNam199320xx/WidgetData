using WidgetData.Domain.Interfaces;

namespace WidgetData.Domain;

public class TenantContext : ITenantContext
{
    public int? CurrentTenantId { get; set; }
    public bool IsSuperAdmin { get; set; }
}
