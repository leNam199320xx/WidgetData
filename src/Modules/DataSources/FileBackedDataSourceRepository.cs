using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;

namespace WidgetData.DataSources;

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
