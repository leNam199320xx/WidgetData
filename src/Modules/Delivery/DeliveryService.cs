using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Delivery;

public class DeliveryService : IDeliveryService
{
    private readonly IDeliveryTargetService _targetService;
    private readonly IDeliveryExecutionService _executionService;
    private readonly IDeliveryDispatcher _dispatcher;

    public DeliveryService(IDeliveryTargetService targetService, IDeliveryExecutionService executionService, IDeliveryDispatcher dispatcher)
    {
        _targetService = targetService;
        _executionService = executionService;
        _dispatcher = dispatcher;
    }

    public DeliveryService(
        ApplicationDbContext context,
        IExportService exportService,
        IHttpClientFactory httpClientFactory,
        IDeliveryTargetRepository targetRepo,
        IDeliveryExecutionRepository executionRepo,
        IEnumerable<IDeliveryChannelStrategy> strategies)
    {
        _targetService = new DeliveryTargetService(targetRepo, LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("DeliveryTargetService"));
        _executionService = new DeliveryExecutionService(executionRepo, LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("DeliveryExecutionService"));
        _dispatcher = new DeliveryDispatcher(targetRepo, executionRepo, strategies, exportService, httpClientFactory, LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("DeliveryDispatcher"));
    }

    public Task<IEnumerable<DeliveryTargetDto>> GetTargetsAsync(int widgetId) => _targetService.GetTargetsAsync(widgetId);
    public Task<DeliveryTargetDto?> GetTargetByIdAsync(int id) => _targetService.GetTargetByIdAsync(id);
    public Task<DeliveryTargetDto> CreateTargetAsync(CreateDeliveryTargetDto dto, string userId) => _targetService.CreateTargetAsync(dto, userId);
    public Task<DeliveryTargetDto?> UpdateTargetAsync(int id, UpdateDeliveryTargetDto dto) => _targetService.UpdateTargetAsync(id, dto);
    public Task<bool> DeleteTargetAsync(int id) => _targetService.DeleteTargetAsync(id);
    public Task<DeliveryExecutionDto> DeliverAsync(int widgetId, int deliveryTargetId, string userId) => _dispatcher.DispatchAsync(widgetId, deliveryTargetId, userId);
    public Task<IEnumerable<DeliveryExecutionDto>> GetExecutionsAsync(int widgetId) => _executionService.GetExecutionsAsync(widgetId);
}
