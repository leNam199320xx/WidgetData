using WidgetData.Application.DTOs;

namespace WidgetData.Application.Interfaces;

public interface IDataSourceService
{
    Task<IEnumerable<DataSourceDto>> GetAllAsync();
    Task<DataSourceDto?> GetByIdAsync(int id);
    Task<DataSourceDto> CreateAsync(CreateDataSourceDto dto, string userId);
    Task<DataSourceDto?> UpdateAsync(int id, UpdateDataSourceDto dto);
    Task<bool> DeleteAsync(int id);
    Task<string> TestConnectionAsync(int id);
}
