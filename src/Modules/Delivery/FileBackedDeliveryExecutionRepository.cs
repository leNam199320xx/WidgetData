using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Delivery;

public class FileBackedDeliveryExecutionRepository : IDeliveryExecutionRepository
{
    private readonly IJsonDeliveryExecutionRepository _repo;

    public FileBackedDeliveryExecutionRepository(IJsonDeliveryExecutionRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<DeliveryExecution>> GetByTargetAsync(int deliveryTargetId) => await _repo.GetByTargetAsync(deliveryTargetId);
    public async Task<IEnumerable<DeliveryExecution>> GetByWidgetAsync(int widgetId)
    {
        var all = await _repo.GetAllAsync();
        return all.Where(e => e.DeliveryTarget != null && e.DeliveryTarget.WidgetId == widgetId).OrderByDescending(e => e.ExecutedAt).ToList();
    }
    public async Task<DeliveryExecution> CreateAsync(DeliveryExecution execution)
    {
        var all = await _repo.GetAllAsync();
        execution.Id = FileBackedRepositoryId.NextId(all.Select(x => x.Id));
        return await _repo.CreateAsync(execution);
    }
    public Task<DeliveryExecution> UpdateAsync(DeliveryExecution execution) => _repo.UpdateAsync(execution);
}
