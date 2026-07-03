using WidgetData.Application.DTOs;

namespace WidgetData.Application.Interfaces;

public interface IDeliveryDispatcher
{
    Task<DeliveryExecutionDto> DispatchAsync(int widgetId, int deliveryTargetId, string userId);
}