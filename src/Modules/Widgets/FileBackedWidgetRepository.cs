using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Widgets;

internal static class FileBackedRepositoryId
{
    public static int NextId(IEnumerable<int> ids) => ids.Any() ? ids.Max() + 1 : 1;
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
