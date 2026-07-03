using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;

namespace WidgetData.Infrastructure.Services;

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