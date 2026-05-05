namespace WidgetData.Domain.Interfaces;

/// <summary>
/// Lưu TenantId của request hiện tại.
/// Null = SuperAdmin (không lọc theo tenant).
/// </summary>
public interface ITenantContext
{
    int? CurrentTenantId { get; set; }
    bool IsSuperAdmin { get; set; }
}
