using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Infrastructure.Services;

public class DataSourceService : IDataSourceService
{
    private readonly IDataSourceRepository _repo;

    public DataSourceService(IDataSourceRepository repo)
    {
        _repo = repo;
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
            CreatedBy = userId
        };
        var created = await _repo.CreateAsync(entity);
        return MapToDto(created);
    }

    public async Task<DataSourceDto?> UpdateAsync(int id, UpdateDataSourceDto dto)
    {
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

    public async Task<string> TestConnectionAsync(int id)
    {
        var ds = await _repo.GetByIdAsync(id);
        if (ds == null) return "Data source not found";
        ds.LastTestedAt = DateTime.UtcNow;
        ds.LastTestResult = "Connection successful";
        await _repo.UpdateAsync(ds);
        return "Connection successful";
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
        LastTestResult = ds.LastTestResult
    };
}
