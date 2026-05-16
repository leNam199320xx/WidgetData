using System.Net.Http;
using System.Threading;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Infrastructure.Services;

public class DataSourceService : IDataSourceService
{
    private readonly IDataSourceRepository _repo;
    private readonly ITenantContext? _tenantContext;
    private readonly IHostEnvironment _hostEnvironment;

    public DataSourceService(IDataSourceRepository repo, IHostEnvironment hostEnvironment, ITenantContext? tenantContext = null)
    {
        _repo = repo;
        _hostEnvironment = hostEnvironment;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<DataSourceDto>> GetAllAsync()
    {
        var items = await _repo.GetAllAsync();
        return items.Select(MapToDto);
    }

    public async Task<DataSourceDto?> GetByIdAsync(int id)
    {
        var item = await _repo.GetByIdAsync(id);
        return item == null ? null : MapToDto(item);
    }

    public async Task<DataSourceDto> CreateAsync(CreateDataSourceDto dto, string userId)
    {
        EnsureJsonOnlySourceType(dto.SourceType);

        var entity = new DataSource
        {
            Name = dto.Name,
            SourceType = dto.SourceType,
            Description = dto.Description,
            ConnectionString = dto.ConnectionString,
            Host = dto.Host,
            Port = dto.Port,
            DatabaseName = dto.DatabaseName,
            Username = dto.Username,
            Password = dto.Password,
            ApiEndpoint = dto.ApiEndpoint,
            ApiKey = dto.ApiKey,
            AdditionalConfig = dto.AdditionalConfig,
            CreatedBy = userId,
            TenantId = _tenantContext?.CurrentTenantId
        };
        var created = await _repo.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<DataSourceDto?> UpdateAsync(int id, UpdateDataSourceDto dto)
    {
        EnsureJsonOnlySourceType(dto.SourceType);

        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return null;
        entity.Name = dto.Name;
        entity.SourceType = dto.SourceType;
        entity.Description = dto.Description;
        entity.ConnectionString = dto.ConnectionString;
        entity.Host = dto.Host;
        entity.Port = dto.Port;
        entity.DatabaseName = dto.DatabaseName;
        entity.Username = dto.Username;
        if (!string.IsNullOrEmpty(dto.Password)) entity.Password = dto.Password;
        entity.ApiEndpoint = dto.ApiEndpoint;
        if (!string.IsNullOrEmpty(dto.ApiKey)) entity.ApiKey = dto.ApiKey;
        entity.AdditionalConfig = dto.AdditionalConfig;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        var updated = await _repo.UpdateAsync(entity);
        return MapToDto(updated);
    }

    public async Task<DataSourceFileUploadDto?> UploadFileAsync(
        int id,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSizeBytes,
        string uploadedBy)
    {
        var ds = await _repo.GetByIdAsync(id);
        if (ds == null) return null;

        if (!CanManage(ds))
            throw new UnauthorizedAccessException("Forbidden to upload file for this data source.");

        if (!IsFileSourceType(ds.SourceType))
            throw new InvalidOperationException("Only CSV/Excel/Json data sources support file upload.");

        if (fileSizeBytes <= 0 || fileSizeBytes > 20 * 1024 * 1024)
            throw new InvalidOperationException("Invalid file size. Maximum allowed size is 20MB.");

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var allowed = ds.SourceType switch
        {
            DataSourceType.Csv => new[] { ".csv" },
            DataSourceType.Excel => new[] { ".xlsx", ".xls" },
            DataSourceType.Json => new[] { ".json" },
            _ => Array.Empty<string>()
        };

        if (!allowed.Contains(ext))
            throw new InvalidOperationException($"Invalid file type for {ds.SourceType}. Allowed: {string.Join(", ", allowed)}");

        var tenantFolder = ds.TenantId?.ToString() ?? "shared";
        var root = Path.Combine(_hostEnvironment.ContentRootPath, "uploads", "datasources", tenantFolder, ds.Id.ToString());
        Directory.CreateDirectory(root);

        var storedFileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(root, storedFileName);

        await using (var target = File.Create(fullPath))
        {
            await fileStream.CopyToAsync(target);
        }

        ds.OriginalFileName = Path.GetFileName(fileName);
        ds.StoredFileName = storedFileName;
        ds.FileContentType = contentType;
        ds.FileSizeBytes = fileSizeBytes;
        ds.FileStoragePath = fullPath;
        ds.FileUploadedAt = DateTime.UtcNow;
        ds.FileUploadedBy = uploadedBy;
        ds.ConnectionString = fullPath;
        ds.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(ds);

        return new DataSourceFileUploadDto
        {
            OriginalFileName = ds.OriginalFileName,
            StoredFileName = ds.StoredFileName,
            ContentType = ds.FileContentType ?? "application/octet-stream",
            FileSizeBytes = ds.FileSizeBytes ?? 0,
            UploadedAt = ds.FileUploadedAt ?? DateTime.UtcNow
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return false;
        await _repo.DeleteAsync(id);
        return true;
    }

    public async Task<string> TestConnectionAsync(int id)
    {
        var ds = await _repo.GetByIdAsync(id);
        if (ds == null) return "Data source not found";

        string result;
        try
        {
            result = ds.SourceType switch
            {
                DataSourceType.RestApi => await TestRestApiAsync(ds.ApiEndpoint, ds.ApiKey),
                DataSourceType.Csv => await TestFileSourceAsync(ds.FileStoragePath ?? ds.ConnectionString, "CSV"),
                DataSourceType.Excel => await TestFileSourceAsync(ds.FileStoragePath ?? ds.ConnectionString, "Excel"),
                DataSourceType.Json => await TestJsonFileAsync(ds.FileStoragePath ?? ds.ConnectionString),
                _ => "Only JSON data source is supported"
            };
        }
        catch (Exception ex)
        {
            result = $"Connection failed: {ex.Message}";
        }

        ds.LastTestedAt = DateTime.UtcNow;
        ds.LastTestResult = result;
        await _repo.UpdateAsync(ds);
        return result;
    }

    private async Task<string> TestRestApiAsync(string? endpoint, string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            return "Connection failed: API endpoint is empty";
        using var http = new HttpClient();
        if (_hostEnvironment.IsDevelopment())
            http.Timeout = Timeout.InfiniteTimeSpan;
        if (!string.IsNullOrWhiteSpace(apiKey))
            http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        var response = await http.GetAsync(endpoint);
        return response.IsSuccessStatusCode
            ? $"Connection successful (HTTP {(int)response.StatusCode})"
            : $"Connection failed: HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
    }

    private static Task<string> TestFileSourceAsync(string? filePath, string sourceType)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Task.FromResult($"Connection failed: {sourceType} file path is empty");
        if (!File.Exists(filePath))
            return Task.FromResult($"Connection failed: {sourceType} file not found: {filePath}");

        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _ = stream.Length;
            return Task.FromResult("Connection successful");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Connection failed: cannot read {sourceType} file - {ex.Message}");
        }
    }

    private static Task<string> TestJsonFileAsync(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Task.FromResult("Connection failed: JSON file path is empty");
        if (!File.Exists(filePath))
            return Task.FromResult($"Connection failed: JSON file not found: {filePath}");

        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var jsonDoc = JsonDocument.Parse(stream);
            return Task.FromResult("Connection successful");
        }
        catch (JsonException ex)
        {
            return Task.FromResult($"Connection failed: invalid JSON format - {ex.Message}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Connection failed: cannot read JSON file - {ex.Message}");
        }
    }

    private static DataSourceDto MapToDto(DataSource ds) => new()
    {
        Id = ds.Id,
        Name = ds.Name,
        SourceType = ds.SourceType,
        Description = ds.Description,
        Host = ds.Host,
        Port = ds.Port,
        DatabaseName = ds.DatabaseName,
        Username = ds.Username,
        ApiEndpoint = ds.ApiEndpoint,
        AdditionalConfig = ds.AdditionalConfig,
        IsActive = ds.IsActive,
        CreatedBy = ds.CreatedBy,
        CreatedAt = ds.CreatedAt,
        LastTestedAt = ds.LastTestedAt,
        LastTestResult = ds.LastTestResult,
        OriginalFileName = ds.OriginalFileName,
        StoredFileName = ds.StoredFileName,
        FileContentType = ds.FileContentType,
        FileSizeBytes = ds.FileSizeBytes,
        FileUploadedAt = ds.FileUploadedAt,
        FileUploadedBy = ds.FileUploadedBy
    };

    private static bool IsFileSourceType(DataSourceType sourceType)
        => sourceType is DataSourceType.Csv or DataSourceType.Excel or DataSourceType.Json;

    private bool CanManage(DataSource ds)
    {
        if (_tenantContext?.IsSuperAdmin == true) return true;
        if (_tenantContext?.CurrentTenantId == null) return true;
        return ds.TenantId == null || ds.TenantId == _tenantContext.CurrentTenantId;
    }

    private static void EnsureJsonOnlySourceType(DataSourceType sourceType)
    {
        if (sourceType != DataSourceType.Json)
            throw new InvalidOperationException("Only JSON data source is supported.");
    }
}
