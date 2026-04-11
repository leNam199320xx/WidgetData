using WidgetData.Domain.Entities;

namespace WidgetData.Domain.Interfaces;

public interface IExecutionRepository
{
    Task<IEnumerable<WidgetExecution>> GetByWidgetIdAsync(int widgetId);
    Task<WidgetExecution?> GetByIdAsync(int id);
    Task<WidgetExecution> CreateAsync(WidgetExecution execution);
    Task<WidgetExecution> UpdateAsync(WidgetExecution execution);
}
