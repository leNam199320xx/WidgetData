using WidgetData.Domain.Enums;

namespace WidgetData.Domain.Entities;

public class DataSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DataSourceType SourceType { get; set; }
    public string? Description { get; set; }
    public string? ConnectionString { get; set; }
    public string? Host { get; set; }
    public int? Port { get; set; }
    public string? DatabaseName { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? ApiEndpoint { get; set; }
    public string? ApiKey { get; set; }
    public string? AdditionalConfig { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastTestedAt { get; set; }
    public string? LastTestResult { get; set; }
    public string? OriginalFileName { get; set; }
    public string? StoredFileName { get; set; }
    public string? FileContentType { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? FileStoragePath { get; set; }
    public DateTime? FileUploadedAt { get; set; }
    public string? FileUploadedBy { get; set; }
    public int? TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public ICollection<Widget> Widgets { get; set; } = new List<Widget>();
}
