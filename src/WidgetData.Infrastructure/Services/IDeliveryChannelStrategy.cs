using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;

namespace WidgetData.Infrastructure.Services;

public interface IDeliveryChannelStrategy
{
    DeliveryType SupportedType { get; }
    Task DeliverAsync(int widgetId, DeliveryTarget target, IExportService exportService, IHttpClientFactory httpClientFactory);
}