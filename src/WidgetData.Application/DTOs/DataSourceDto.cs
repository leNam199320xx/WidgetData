using WidgetData.Domain.Enums;

namespace WidgetData.Application.DTOs;

public class DataSourceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DataSourceType SourceType { get; set; }
    public string? Description { get; set; }
    public string? Host { get; set; }
    public int? Port { get; set; }
    public string? DatabaseName { get; set; }
    public string? Username { get; set; }
    public string? ApiEndpoint { get; set; }
    public string? AdditionalConfig { get; set; }
    public bool IsActive { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastTestedAt { get; set; }
    public string? LastTestResult { get; set; }
}

public class CreateDataSourceDto
{
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
}

public class UpdateDataSourceDto : CreateDataSourceDto
{
    public bool IsActive { get; set; } = true;
}
