using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;

namespace WidgetData.Domain.Interfaces;

public record ExecutionDashboardStats(
    int Total,
    int Successful,
    int Failed,
    IEnumerable<WidgetExecution> Recent);

public interface IExecutionRepository
{
    Task<IEnumerable<WidgetExecution>> GetAllAsync();
    Task<int> CountAsync();
    Task<int> CountByStatusAsync(ExecutionStatus status);
    Task<IEnumerable<WidgetExecution>> GetRecentAsync(int days, int limit);
    Task<IEnumerable<WidgetExecution>> GetByWidgetIdAsync(int widgetId);
    Task<WidgetExecution?> GetByIdAsync(int id);
    Task<WidgetExecution> CreateAsync(WidgetExecution execution);
    Task<WidgetExecution> UpdateAsync(WidgetExecution execution);
    Task<ExecutionDashboardStats> GetDashboardStatsAsync(int days, int limit);
}
