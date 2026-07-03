using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Infrastructure.Services;

public interface IDataSourceStrategy
{
    bool CanHandle(DataSourceType sourceType);
    Task<object> LoadDataAsync(Widget widget, DataSource ds, IHttpClientFactory httpClientFactory);
}