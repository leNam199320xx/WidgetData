using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data.Json.Repositories;
using Microsoft.Extensions.Logging;

namespace WidgetData.Infrastructure.Repositories;

internal static class FileBackedRepositoryId
{
    public static int NextId(IEnumerable<int> ids) => ids.Any() ? ids.Max() + 1 : 1;
}

public class FileBackedDataSourceRepository : IDataSourceRepository
{
    private readonly IJsonDataSourceRepository _repo;
    private readonly ITenantContext? _tenantContext;

    public FileBackedDataSourceRepository(IJsonDataSourceRepository repo, ITenantContext? tenantContext = null)
    {
        _repo = repo;
        _tenantContext = tenantContext;
    }

    private async Task<IEnumerable<DataSource>> GetCurrentDataSourcesAsync()
    {
        if (_tenantContext?.IsSuperAdmin == true || _tenantContext?.CurrentTenantId == null)
            return await _repo.GetAllAsync();

        return await _repo.GetByTenantAsync(_tenantContext.CurrentTenantId);
    }

    public async Task<IEnumerable<DataSource>> GetAllAsync()
        => await GetCurrentDataSourcesAsync();

    public async Task<int> CountAsync()
        => (await GetCurrentDataSourcesAsync()).Count();

    public async Task<int> CountActiveAsync()
        => (await GetCurrentDataSourcesAsync()).Count(d => d.IsActive);

    public async Task<(int Total, int Active)> GetCountsAsync()
    {
        var all = (await GetCurrentDataSourcesAsync()).ToList();
        return (all.Count, all.Count(d => d.IsActive));
    }

    public Task<DataSource?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);

    public async Task<DataSource> CreateAsync(DataSource dataSource)
    {
        var all = await _repo.GetAllAsync();
        dataSource.Id = FileBackedRepositoryId.NextId(all.Select(x => x.Id));
        return await _repo.CreateAsync(dataSource);
    }

    public Task<DataSource> UpdateAsync(DataSource dataSource) => _repo.UpdateAsync(dataSource);

    public async Task DeleteAsync(int id) => await _repo.DeleteAsync(id);
}

public class FileBackedWidgetRepository : IWidgetRepository
{
    private readonly IJsonWidgetRepository _repo;
    private readonly IJsonDataSourceRepository _dataSourceRepo;
    private readonly ITenantContext? _tenantContext;

    public FileBackedWidgetRepository(
        IJsonWidgetRepository repo,
        IJsonDataSourceRepository dataSourceRepo,
        ITenantContext? tenantContext = null)
    {
        _repo = repo;
        _dataSourceRepo = dataSourceRepo;
        _tenantContext = tenantContext;
    }

    private async Task<IEnumerable<Widget>> GetCurrentWidgetsAsync()
        => (_tenantContext?.IsSuperAdmin == true || _tenantContext?.CurrentTenantId == null)
            ? await _repo.GetAllAsync()
            : await _repo.GetByTenantAsync(_tenantContext.CurrentTenantId);

    public async Task<IEnumerable<Widget>> GetAllAsync()
    {
        var widgets = await GetCurrentWidgetsAsync();

        var dataSources = await _dataSourceRepo.GetAllAsync();
        var dataSourceMap = dataSources.ToDictionary(d => d.Id);
        foreach (var widget in widgets)
            widget.DataSource = dataSourceMap.GetValueOrDefault(widget.DataSourceId) ?? new DataSource { Id = widget.DataSourceId };

        return widgets;
    }

    public async Task<int> CountAsync()
        => (await GetCurrentWidgetsAsync()).Count();

    public async Task<int> CountActiveAsync()
        => (await GetCurrentWidgetsAsync()).Count(w => w.IsActive);

    public async Task<(int Total, int Active)> GetCountsAsync()
    {
        var all = (await GetCurrentWidgetsAsync()).ToList();
        return (all.Count, all.Count(w => w.IsActive));
    }

    public async Task<Widget?> GetByIdAsync(int id)
    {
        var widget = await _repo.GetByIdAsync(id);
        if (widget == null) return null;
        widget.DataSource = await _dataSourceRepo.GetByIdAsync(widget.DataSourceId) ?? new DataSource { Id = widget.DataSourceId };
        return widget;
    }

    public async Task<Widget> CreateAsync(Widget widget)
    {
        var all = await _repo.GetAllAsync();
        widget.Id = FileBackedRepositoryId.NextId(all.Select(x => x.Id));
        return await _repo.CreateAsync(widget);
    }

    public Task<Widget> UpdateAsync(Widget widget) => _repo.UpdateAsync(widget);

    public async Task DeleteAsync(int id) => await _repo.DeleteAsync(id);
}

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

public class FileBackedPageRepository : IPageRepository
{
    private readonly IJsonPageRepository _pageRepo;
    private readonly IJsonPageVersionRepository _versionRepo;
    private readonly IJsonPageWidgetRepository _pageWidgetRepo;
    private readonly IJsonWidgetRepository _widgetRepo;
    private readonly ITenantContext? _tenantContext;

    public FileBackedPageRepository(
        IJsonPageRepository pageRepo,
        IJsonPageVersionRepository versionRepo,
        IJsonPageWidgetRepository pageWidgetRepo,
        IJsonWidgetRepository widgetRepo,
        ITenantContext? tenantContext = null)
    {
        _pageRepo = pageRepo;
        _versionRepo = versionRepo;
        _pageWidgetRepo = pageWidgetRepo;
        _widgetRepo = widgetRepo;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<Page>> GetAllAsync(int? tenantId = null, ScreenType? screenType = null)
    {
        var pages = await _pageRepo.GetAllAsync();

        IEnumerable<Page> filtered = pages;
        if (tenantId.HasValue)
            filtered = filtered.Where(p => p.TenantId == tenantId.Value);
        else if (!(_tenantContext?.IsSuperAdmin == true || _tenantContext?.CurrentTenantId == null))
            filtered = filtered.Where(p => p.TenantId == _tenantContext!.CurrentTenantId);

        if (screenType.HasValue)
            filtered = filtered.Where(p => p.ScreenType == screenType.Value);

        var result = filtered.OrderBy(p => p.Title).ToList();
        foreach (var page in result)
            await LoadWidgetsAsync(page);

        return result;
    }

    public async Task<Page?> GetByIdAsync(int id)
    {
        var page = await _pageRepo.GetByIdAsync(id);
        if (page == null) return null;
        await LoadWidgetsAsync(page);
        return page;
    }

    public async Task<Page?> GetBySlugAsync(string slug, int? tenantId = null)
    {
        var pages = await GetAllAsync(tenantId);
        var page = pages.FirstOrDefault(p => p.Slug == slug && p.IsActive);
        if (page == null) return null;
        await LoadWidgetsAsync(page);
        return page;
    }

    public async Task<Page> CreateAsync(Page page)
    {
        var all = await _pageRepo.GetAllAsync();
        page.Id = FileBackedRepositoryId.NextId(all.Select(x => x.Id));
        return await _pageRepo.CreateAsync(page);
    }

    public Task<Page> UpdateAsync(Page page) => _pageRepo.UpdateAsync(page);

    public async Task<PageVersion> CreateVersionAsync(PageVersion pageVersion)
    {
        var all = await _versionRepo.GetAllAsync();
        pageVersion.Id = FileBackedRepositoryId.NextId(all.Select(x => x.Id));
        return await _versionRepo.CreateAsync(pageVersion);
    }

    public async Task<PageVersion?> GetVersionAsync(int pageId, int versionNumber)
    {
        var versions = await _versionRepo.GetByPageAsync(pageId);
        return versions.FirstOrDefault(v => v.VersionNumber == versionNumber);
    }

    public async Task<IEnumerable<PageVersion>> GetVersionsAsync(int pageId)
        => await _versionRepo.GetByPageAsync(pageId);

    public async Task DeleteAsync(int id)
    {
        await _pageRepo.DeleteAsync(id);
        var versions = await _versionRepo.GetByPageAsync(id);
        foreach (var version in versions)
            await _versionRepo.DeleteAsync(version.Id);

        var widgets = await _pageWidgetRepo.GetByPageAsync(id);
        foreach (var widget in widgets)
            await _pageWidgetRepo.DeleteAsync(widget.Id);
    }

    public async Task AddWidgetAsync(int pageId, int widgetId, int position, int width)
    {
        var existing = (await _pageWidgetRepo.GetByPageAsync(pageId))
            .FirstOrDefault(pw => pw.WidgetId == widgetId);

        if (existing != null)
        {
            existing.Position = position;
            existing.Width = width;
            await _pageWidgetRepo.UpdateAsync(existing);
            return;
        }

        var all = await _pageWidgetRepo.GetAllAsync();
        await _pageWidgetRepo.CreateAsync(new PageWidget
        {
            Id = FileBackedRepositoryId.NextId(all.Select(x => x.Id)),
            PageId = pageId,
            WidgetId = widgetId,
            Position = position,
            Width = width
        });
    }

    public async Task RemoveWidgetAsync(int pageId, int widgetId)
    {
        var existing = (await _pageWidgetRepo.GetByPageAsync(pageId))
            .FirstOrDefault(pw => pw.WidgetId == widgetId);
        if (existing != null)
            await _pageWidgetRepo.DeleteAsync(existing.Id);
    }

    public async Task UpdateWidgetLayoutAsync(int pageId, int widgetId, int position, int width)
    {
        var existing = (await _pageWidgetRepo.GetByPageAsync(pageId))
            .FirstOrDefault(pw => pw.WidgetId == widgetId);
        if (existing != null)
        {
            existing.Position = position;
            existing.Width = width;
            await _pageWidgetRepo.UpdateAsync(existing);
        }
    }

    private async Task LoadWidgetsAsync(Page page)
    {
        var links = await _pageWidgetRepo.GetByPageAsync(page.Id);
        var widgets = await _widgetRepo.GetAllAsync();
        var widgetMap = widgets.ToDictionary(w => w.Id);

        page.PageWidgets = links
            .OrderBy(x => x.Position)
            .Select(link =>
            {
                if (widgetMap.TryGetValue(link.WidgetId, out var widget))
                    link.Widget = widget;
                return link;
            })
            .ToList();
    }
}
