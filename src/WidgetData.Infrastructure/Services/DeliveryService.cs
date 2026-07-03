using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MimeKit;
using Renci.SshNet;
using Telegram.Bot;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Infrastructure.Data;
using WidgetData.Infrastructure.Repositories;

namespace WidgetData.Infrastructure.Services;

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

    public DeliveryService(ApplicationDbContext context, IExportService exportService, IHttpClientFactory httpClientFactory)
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var targetRepo = new DeliveryTargetRepository(context);
        var executionRepo = new DeliveryExecutionRepository(context);
        var strategies = new List<IDeliveryChannelStrategy>
        {
            new EmailDeliveryChannelStrategy(),
            new SftpDeliveryChannelStrategy(),
            new SshDeliveryChannelStrategy(),
            new HttpApiDeliveryChannelStrategy(),
            new TelegramDeliveryChannelStrategy(),
            new ZaloDeliveryChannelStrategy(),
            new FileDeliveryChannelStrategy()
        };
        _targetService = new DeliveryTargetService(targetRepo, loggerFactory.CreateLogger("DeliveryTargetService"));
        _executionService = new DeliveryExecutionService(executionRepo, loggerFactory.CreateLogger("DeliveryExecutionService"));
        _dispatcher = new DeliveryDispatcher(targetRepo, executionRepo, strategies, exportService, httpClientFactory, loggerFactory.CreateLogger("DeliveryDispatcher"));
    }

    public Task<IEnumerable<DeliveryTargetDto>> GetTargetsAsync(int widgetId) => _targetService.GetTargetsAsync(widgetId);
    public Task<DeliveryTargetDto?> GetTargetByIdAsync(int id) => _targetService.GetTargetByIdAsync(id);
    public Task<DeliveryTargetDto> CreateTargetAsync(CreateDeliveryTargetDto dto, string userId) => _targetService.CreateTargetAsync(dto, userId);
    public Task<DeliveryTargetDto?> UpdateTargetAsync(int id, UpdateDeliveryTargetDto dto) => _targetService.UpdateTargetAsync(id, dto);
    public Task<bool> DeleteTargetAsync(int id) => _targetService.DeleteTargetAsync(id);
    public Task<DeliveryExecutionDto> DeliverAsync(int widgetId, int deliveryTargetId, string userId) => _dispatcher.DispatchAsync(widgetId, deliveryTargetId, userId);
    public Task<IEnumerable<DeliveryExecutionDto>> GetExecutionsAsync(int widgetId) => _executionService.GetExecutionsAsync(widgetId);
}