using WidgetData.Domain.Entities;

namespace WidgetData.Domain.Interfaces;

public interface IDeliveryExecutionRepository
{
    Task<IEnumerable<DeliveryExecution>> GetByTargetAsync(int deliveryTargetId);
    Task<IEnumerable<DeliveryExecution>> GetByWidgetAsync(int widgetId);
    Task<DeliveryExecution> CreateAsync(DeliveryExecution execution);
    Task<DeliveryExecution> UpdateAsync(DeliveryExecution execution);
}