using WidgetData.Domain.Interfaces;

namespace WidgetData.Infrastructure.Services;

/// <summary>
/// Scoped service lưu TenantId của request đang xử lý.
/// Được set bởi TenantContextMiddleware dựa trên JWT claim.
/// </summary>
public class TenantContext : ITenantContext
{
    public int? CurrentTenantId { get; set; }
    public bool IsSuperAdmin { get; set; }
}
