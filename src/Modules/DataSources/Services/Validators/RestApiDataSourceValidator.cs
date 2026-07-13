using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Application.Interfaces;

namespace WidgetData.DataSources;

public class RestApiDataSourceValidator : IDataSourceValidator
{
    public bool CanValidate(DataSourceType sourceType) => sourceType == DataSourceType.RestApi;

    public Task ValidateAsync(DataSource dataSource)
    {
        if (string.IsNullOrWhiteSpace(dataSource.ApiEndpoint))
            throw new InvalidOperationException("REST API data source requires a valid endpoint.");
        return Task.CompletedTask;
    }
}
