using Microsoft.Extensions.Logging;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Widgets;

public class FileBackedWidgetConfigArchiveRepository : IWidgetConfigArchiveRepository
{
    private readonly IJsonWidgetConfigArchiveRepository _repo;
    private readonly IJsonWidgetRepository _widgetRepo;
    private readonly ILogger<FileBackedWidgetConfigArchiveRepository> _logger;

    public FileBackedWidgetConfigArchiveRepository(
        IJsonWidgetConfigArchiveRepository repo,
        IJsonWidgetRepository widgetRepo,
        ILogger<FileBackedWidgetConfigArchiveRepository> logger)
    {
        _repo = repo;
        _widgetRepo = widgetRepo;
        _logger = logger;
    }

    public async Task<IEnumerable<WidgetConfigArchive>> GetAllAsync()
    {
        var items = await _repo.GetAllAsync();
        foreach (var item in items)
        {
            var widget = await _widgetRepo.GetByIdAsync(item.WidgetId);
            if (widget == null)
            {
                _logger.LogWarning("Missing widget reference for archive {ArchiveId}, widget {WidgetId}", item.Id, item.WidgetId);
                widget = new Widget { Id = item.WidgetId, Name = $"Widget {item.WidgetId}" };
            }
            item.Widget = widget;
        }
        return items.OrderByDescending(x => x.ArchivedAt).ToList();
    }

    public async Task<IEnumerable<WidgetConfigArchive>> GetByWidgetIdAsync(int widgetId)
        => await _repo.GetByWidgetAsync(widgetId);

    public Task<WidgetConfigArchive?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);

    public async Task<WidgetConfigArchive> CreateAsync(WidgetConfigArchive archive)
    {
        var all = await _repo.GetAllAsync();
        archive.Id = FileBackedRepositoryId.NextId(all.Select(x => x.Id));
        return await _repo.CreateAsync(archive);
    }

    public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
}
