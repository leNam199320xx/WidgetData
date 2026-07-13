using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Widgets;

public class FileBackedExecutionRepository : IExecutionRepository
{
    private readonly IJsonExecutionRepository _repo;

    public FileBackedExecutionRepository(IJsonExecutionRepository repo)
    {
        _repo = repo;
    }

    public Task<WidgetExecution?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);

    public async Task<IEnumerable<WidgetExecution>> GetAllAsync()
        => await _repo.GetAllAsync();

    public async Task<int> CountAsync()
        => await _repo.CountAsync();

    public async Task<int> CountByStatusAsync(ExecutionStatus status)
        => await _repo.CountByStatusAsync(status);

    public async Task<IEnumerable<WidgetExecution>> GetRecentAsync(int days, int limit)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        return (await _repo.GetByDateRangeAsync(since, DateTime.UtcNow.AddDays(1)))
            .OrderByDescending(e => e.StartedAt)
            .Take(limit);
    }

    public async Task<ExecutionDashboardStats> GetDashboardStatsAsync(int days, int limit)
    {
        var all = await _repo.GetAllAsync();
        var since = DateTime.UtcNow.AddDays(-days);
        var total = all.Count;
        var successful = all.Count(e => e.Status == ExecutionStatus.Success);
        var failed = all.Count(e => e.Status == ExecutionStatus.Failed);
        var recent = all
            .Where(e => e.StartedAt >= since)
            .OrderByDescending(e => e.StartedAt)
            .Take(limit)
            .ToList();
        return new ExecutionDashboardStats(total, successful, failed, recent);
    }

    public async Task<IEnumerable<WidgetExecution>> GetByWidgetIdAsync(int widgetId)
        => await _repo.GetByWidgetAsync(widgetId, 100);

    public async Task<WidgetExecution> CreateAsync(WidgetExecution execution)
    {
        var all = await _repo.GetAllAsync();
        execution.Id = FileBackedRepositoryId.NextId(all.Select(x => x.Id));
        return await _repo.CreateAsync(execution);
    }

    public Task<WidgetExecution> UpdateAsync(WidgetExecution execution) => _repo.UpdateAsync(execution);
}
