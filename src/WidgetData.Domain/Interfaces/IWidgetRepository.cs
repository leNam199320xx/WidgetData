using WidgetData.Domain.Entities;

namespace WidgetData.Domain.Interfaces;

public interface IWidgetRepository
{
    Task<IEnumerable<Widget>> GetAllAsync();
    Task<int> CountAsync();
    Task<int> CountActiveAsync();
    Task<Widget?> GetByIdAsync(int id);
    Task<Widget> CreateAsync(Widget widget);
    Task<Widget> UpdateAsync(Widget widget);
    Task DeleteAsync(int id);
}
