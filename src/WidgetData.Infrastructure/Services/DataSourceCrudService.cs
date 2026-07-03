using System.Text.Json;
using Microsoft.Extensions.Logging;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Services;

namespace WidgetData.Infrastructure.Services;

public class DataSourceCrudService : IDataSourceCrudService
{
    private readonly IDataSourceRepository _repo;
    private readonly IEnumerable<IDataSourceValidator> _validators;
    private readonly ILogger _logger;
    private readonly ITenantContext? _tenantContext;

    public DataSourceCrudService(IDataSourceRepository repo, IEnumerable<IDataSourceValidator> validators,
        ILogger logger, ITenantContext? tenantContext = null)
    {
        _repo = repo;
        _validators = validators;
        _logger = logger;
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
        EnsureSupportedSourceType(dto.SourceType);
        await ValidateAsync(dto);

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
        EnsureSupportedSourceType(dto.SourceType);
        await ValidateAsync(dto);

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

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return false;
        await _repo.DeleteAsync(id);
        return true;
    }

    private async Task ValidateAsync(CreateDataSourceDto dto)
    {
        var validator = _validators.FirstOrDefault(v => v.CanValidate(dto.SourceType));
        if (validator == null) return;
        var ds = new DataSource { SourceType = dto.SourceType, ConnectionString = dto.ConnectionString, FileStoragePath = null, ApiEndpoint = dto.ApiEndpoint };
        await validator.ValidateAsync(ds);
    }

    private async Task ValidateAsync(UpdateDataSourceDto dto)
    {
        var validator = _validators.FirstOrDefault(v => v.CanValidate(dto.SourceType));
        if (validator == null) return;
        var ds = new DataSource { SourceType = dto.SourceType, ConnectionString = dto.ConnectionString, FileStoragePath = null, ApiEndpoint = dto.ApiEndpoint };
        await validator.ValidateAsync(ds);
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

    private static void EnsureSupportedSourceType(DataSourceType sourceType)
    {
        if (sourceType is not (DataSourceType.Json or DataSourceType.Csv or DataSourceType.Excel or DataSourceType.RestApi))
            throw new InvalidOperationException("Only JSON, CSV, Excel and REST API data sources are supported.");
    }
}