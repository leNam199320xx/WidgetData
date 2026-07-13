using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Delivery;

internal static class FileBackedRepositoryId
{
    public static int NextId(IEnumerable<int> ids) => ids.Any() ? ids.Max() + 1 : 1;
}

public class FileBackedDeliveryTargetRepository : IDeliveryTargetRepository
{
    private readonly IJsonDeliveryTargetRepository _repo;

    public FileBackedDeliveryTargetRepository(IJsonDeliveryTargetRepository repo)
    {
        _repo = repo;
    }

    public Task<IEnumerable<DeliveryTarget>> GetAllAsync() => _repo.GetAllAsync().ContinueWith(t => (IEnumerable<DeliveryTarget>)t.Result);
    public Task<int> CountAsync() => _repo.GetAllAsync().ContinueWith(t => t.Result.Count);
    public Task<int> CountActiveAsync() => _repo.GetAllAsync().ContinueWith(t => t.Result.Count(d => d.IsEnabled));
    public Task<(int Total, int Active)> GetCountsAsync() => _repo.GetAllAsync().ContinueWith(t => (t.Result.Count, t.Result.Count(d => d.IsEnabled)));
    public Task<DeliveryTarget?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
    public async Task<DeliveryTarget> CreateAsync(DeliveryTarget target)
    {
        var all = await _repo.GetAllAsync();
        target.Id = FileBackedRepositoryId.NextId(all.Select(x => x.Id));
        return await _repo.CreateAsync(target);
    }
    public Task<DeliveryTarget> UpdateAsync(DeliveryTarget target) => _repo.UpdateAsync(target);
    public async Task DeleteAsync(int id) => await _repo.DeleteAsync(id);
    public async Task<IEnumerable<DeliveryTarget>> GetByWidgetAsync(int widgetId) => await _repo.GetByWidgetAsync(widgetId);
}
