using Microsoft.Extensions.Logging;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Infrastructure.Services;

public class DeliveryExecutionService : IDeliveryExecutionService
{
    private readonly IDeliveryExecutionRepository _repo;
    private readonly ILogger _logger;

    public DeliveryExecutionService(IDeliveryExecutionRepository repo, ILogger logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<IEnumerable<DeliveryExecutionDto>> GetExecutionsAsync(int widgetId)
    {
        var executions = await _repo.GetByWidgetAsync(widgetId);
        return executions.Select(MapToDto);
    }

    private static DeliveryExecutionDto MapToDto(DeliveryExecution e) => new()
    {
        Id = e.Id,
        DeliveryTargetId = e.DeliveryTargetId,
        Status = e.Status,
        Message = e.Message,
        TriggeredBy = e.TriggeredBy,
        ExecutedAt = e.ExecutedAt
    };
}