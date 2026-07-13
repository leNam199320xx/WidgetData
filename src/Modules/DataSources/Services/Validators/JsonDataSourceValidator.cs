using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Application.Interfaces;

namespace WidgetData.DataSources;

public class JsonDataSourceValidator : IDataSourceValidator
{
    public bool CanValidate(DataSourceType sourceType) => sourceType == DataSourceType.Json;

    public Task ValidateAsync(DataSource dataSource)
    {
        var path = dataSource.FileStoragePath ?? dataSource.ConnectionString;
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("JSON data source requires a valid file path.");
        return Task.CompletedTask;
    }
}
