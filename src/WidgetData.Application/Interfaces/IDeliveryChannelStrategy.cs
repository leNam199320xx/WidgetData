using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;

namespace WidgetData.Application.Interfaces;

public interface IDeliveryChannelStrategy
{
    DeliveryType SupportedType { get; }
    Task DeliverAsync(int widgetId, DeliveryTarget target, IExportService exportService, IHttpClientFactory httpClientFactory);
}
