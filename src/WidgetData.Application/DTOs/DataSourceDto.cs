using System.ComponentModel.DataAnnotations;
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
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public DataSourceType SourceType { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(1000)]
    public string? ConnectionString { get; set; }

    [StringLength(253)]
    public string? Host { get; set; }

    [Range(1, 65535)]
    public int? Port { get; set; }

    [StringLength(200)]
    public string? DatabaseName { get; set; }

    [StringLength(200)]
    public string? Username { get; set; }

    [StringLength(500)]
    public string? Password { get; set; }

    [Url]
    [StringLength(2048)]
    public string? ApiEndpoint { get; set; }

    [StringLength(500)]
    public string? ApiKey { get; set; }

    [StringLength(2000)]
    public string? AdditionalConfig { get; set; }
}

public class UpdateDataSourceDto : CreateDataSourceDto
{
    public bool IsActive { get; set; } = true;
}
