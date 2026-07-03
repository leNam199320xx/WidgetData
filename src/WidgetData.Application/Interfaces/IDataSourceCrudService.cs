using WidgetData.Application.DTOs;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;

namespace WidgetData.Application.Interfaces;

public interface IDataSourceCrudService
{
    Task<IEnumerable<DataSourceDto>> GetAllAsync();
    Task<DataSourceDto?> GetByIdAsync(int id);
    Task<DataSourceDto> CreateAsync(CreateDataSourceDto dto, string userId);
    Task<DataSourceDto?> UpdateAsync(int id, UpdateDataSourceDto dto);
    Task<bool> DeleteAsync(int id);
}