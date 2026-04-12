using WidgetData.Application.DTOs;

namespace WidgetData.Application.Interfaces;

public interface IDeliveryService
{
    Task<IEnumerable<DeliveryTargetDto>> GetTargetsAsync(int widgetId);
    Task<DeliveryTargetDto?> GetTargetByIdAsync(int id);
    Task<DeliveryTargetDto> CreateTargetAsync(CreateDeliveryTargetDto dto, string userId);
    Task<DeliveryTargetDto?> UpdateTargetAsync(int id, UpdateDeliveryTargetDto dto);
    Task<bool> DeleteTargetAsync(int id);
    Task<DeliveryExecutionDto> DeliverAsync(int widgetId, int deliveryTargetId, string userId);
    Task<IEnumerable<DeliveryExecutionDto>> GetExecutionsAsync(int widgetId);
}
