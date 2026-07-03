using Microsoft.Extensions.Logging;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Infrastructure.Services;

public class DeliveryDispatcher : IDeliveryDispatcher
{
    private readonly IDeliveryTargetRepository _targetRepo;
    private readonly IDeliveryExecutionRepository _executionRepo;
    private readonly IEnumerable<IDeliveryChannelStrategy> _strategies;
    private readonly IExportService _exportService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    public DeliveryDispatcher(
        IDeliveryTargetRepository targetRepo,
        IDeliveryExecutionRepository executionRepo,
        IEnumerable<IDeliveryChannelStrategy> strategies,
        IExportService exportService,
        IHttpClientFactory httpClientFactory,
        ILogger logger)
    {
        _targetRepo = targetRepo;
        _executionRepo = executionRepo;
        _strategies = strategies;
        _exportService = exportService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<DeliveryExecutionDto> DispatchAsync(int widgetId, int deliveryTargetId, string userId)
    {
        var target = await _targetRepo.GetByIdAsync(deliveryTargetId)
            ?? throw new KeyNotFoundException($"Delivery target {deliveryTargetId} not found");

        var execution = new DeliveryExecution
        {
            DeliveryTargetId = deliveryTargetId,
            Status = ExecutionStatus.Running,
            TriggeredBy = userId,
            ExecutedAt = DateTime.UtcNow
        };
        await _executionRepo.CreateAsync(execution);

        try
        {
            var strategy = _strategies.FirstOrDefault(s => s.SupportedType == target.Type)
                ?? throw new NotSupportedException($"Delivery type '{target.Type}' is not supported");

            await strategy.DeliverAsync(widgetId, target, _exportService, _httpClientFactory);
            execution.Status = ExecutionStatus.Success;
            execution.Message = $"Delivered via {target.Type} at {DateTime.UtcNow:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delivery failed for target {TargetId}", deliveryTargetId);
            execution.Status = ExecutionStatus.Failed;
            execution.Message = ex.Message;
        }

        await _executionRepo.UpdateAsync(execution);
        return new DeliveryExecutionDto
        {
            Id = execution.Id,
            DeliveryTargetId = execution.DeliveryTargetId,
            Status = execution.Status,
            Message = execution.Message,
            TriggeredBy = execution.TriggeredBy,
            ExecutedAt = execution.ExecutedAt
        };
    }
}