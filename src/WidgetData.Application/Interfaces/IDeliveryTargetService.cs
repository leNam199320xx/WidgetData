using WidgetData.Application.DTOs;

namespace WidgetData.Application.Interfaces;

public interface IDeliveryTargetService
{
    Task<IEnumerable<DeliveryTargetDto>> GetTargetsAsync(int widgetId);
    Task<DeliveryTargetDto?> GetTargetByIdAsync(int id);
    Task<DeliveryTargetDto> CreateTargetAsync(CreateDeliveryTargetDto dto, string userId);
    Task<DeliveryTargetDto?> UpdateTargetAsync(int id, UpdateDeliveryTargetDto dto);
    Task<bool> DeleteTargetAsync(int id);
}