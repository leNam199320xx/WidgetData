using WidgetData.Domain.Entities;

namespace WidgetData.Domain.Interfaces;

public interface IDataSourceRepository
{
    Task<IEnumerable<DataSource>> GetAllAsync();
    Task<DataSource?> GetByIdAsync(int id);
    Task<DataSource> CreateAsync(DataSource dataSource);
    Task<DataSource> UpdateAsync(DataSource dataSource);
    Task DeleteAsync(int id);
}
