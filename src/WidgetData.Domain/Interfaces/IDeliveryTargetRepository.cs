using WidgetData.Domain.Entities;

namespace WidgetData.Domain.Interfaces;

public interface IDeliveryTargetRepository
{
    Task<IEnumerable<DeliveryTarget>> GetAllAsync();
    Task<DeliveryTarget?> GetByIdAsync(int id);
    Task<IEnumerable<DeliveryTarget>> GetByWidgetAsync(int widgetId);
    Task<DeliveryTarget> CreateAsync(DeliveryTarget target);
    Task<DeliveryTarget> UpdateAsync(DeliveryTarget target);
    Task DeleteAsync(int id);
}