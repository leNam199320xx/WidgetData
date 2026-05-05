namespace WidgetData.Domain.Entities;

/// <summary>
/// Đại diện cho một khách hàng (tenant) trong hệ thống multi-tenant.
/// Mỗi tenant có thể tự host hoặc sử dụng shared host của WidgetData.
/// </summary>
public class Tenant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Slug dùng để phân biệt tenant trong URL hoặc subdomain.</summary>
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    /// <summary>Gói dịch vụ: free, starter, pro, enterprise...</summary>
    public string Plan { get; set; } = "free";
    public string? ContactEmail { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public ICollection<DataSource> DataSources { get; set; } = new List<DataSource>();
    public ICollection<Widget> Widgets { get; set; } = new List<Widget>();
    public ICollection<Page> Pages { get; set; } = new List<Page>();
}
