using Microsoft.Extensions.Logging;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;

namespace WidgetData.CrossCutting;

internal static class FileBackedRepositoryId
{
    public static int NextId(IEnumerable<int> ids) => ids.Any() ? ids.Max() + 1 : 1;
}

public class FileBackedScheduleRepository : IScheduleRepository
{
    private readonly IJsonScheduleRepository _repo;
    private readonly IJsonWidgetRepository _widgetRepo;
    private readonly ILogger<FileBackedScheduleRepository> _logger;

    public FileBackedScheduleRepository(
        IJsonScheduleRepository repo,
        IJsonWidgetRepository widgetRepo,
        ILogger<FileBackedScheduleRepository> logger)
    {
        _repo = repo;
        _widgetRepo = widgetRepo;
        _logger = logger;
    }

    public async Task<IEnumerable<WidgetSchedule>> GetAllAsync()
    {
        var items = await _repo.GetAllAsync();
        foreach (var item in items)
        {
            var widget = await _widgetRepo.GetByIdAsync(item.WidgetId);
            if (widget == null)
            {
                _logger.LogWarning("Missing widget reference for schedule {ScheduleId}, widget {WidgetId}", item.Id, item.WidgetId);
                widget = new Widget { Id = item.WidgetId, Name = $"Widget {item.WidgetId}" };
            }
            item.Widget = widget;
        }
        return items;
    }

    public async Task<int> CountAsync()
        => await _repo.CountAsync();

    public async Task<int> CountEnabledAsync()
        => await _repo.CountEnabledAsync();

    public async Task<(int Total, int Enabled)> GetCountsAsync()
    {
        var all = (await _repo.GetAllAsync()).ToList();
        return (all.Count, all.Count(s => s.IsEnabled));
    }

    public async Task<IEnumerable<WidgetSchedule>> GetByWidgetIdAsync(int widgetId)
        => await _repo.GetByWidgetAsync(widgetId);

    public async Task<IEnumerable<WidgetSchedule>> GetDueAsync(DateTime asOf)
    {
        var items = await _repo.GetAllAsync();
        return items.Where(s => s.IsEnabled && s.NextRunAt != null && s.NextRunAt <= asOf).ToList();
    }

    public async Task<WidgetSchedule?> GetByIdAsync(int id)
    {
        var item = await _repo.GetByIdAsync(id);
        if (item == null) return null;
        var widget = await _widgetRepo.GetByIdAsync(item.WidgetId);
        if (widget == null)
        {
            _logger.LogWarning("Missing widget reference for schedule {ScheduleId}, widget {WidgetId}", item.Id, item.WidgetId);
            widget = new Widget { Id = item.WidgetId, Name = $"Widget {item.WidgetId}" };
        }
        item.Widget = widget;
        return item;
    }

    public async Task<WidgetSchedule> CreateAsync(WidgetSchedule schedule)
    {
        var all = await _repo.GetAllAsync();
        schedule.Id = FileBackedRepositoryId.NextId(all.Select(x => x.Id));
        return await _repo.CreateAsync(schedule);
    }

    public Task<WidgetSchedule> UpdateAsync(WidgetSchedule schedule) => _repo.UpdateAsync(schedule);

    public async Task DeleteAsync(int id) => await _repo.DeleteAsync(id);
}
