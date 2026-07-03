using WidgetData.Application.DTOs;

namespace WidgetData.Application.Interfaces;

public interface IDeliveryExecutionService
{
    Task<IEnumerable<DeliveryExecutionDto>> GetExecutionsAsync(int widgetId);
}