using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;

namespace WidgetData.Infrastructure.Services;

public class CsvDataSourceValidator : IDataSourceValidator
{
    public bool CanValidate(DataSourceType sourceType) => sourceType == DataSourceType.Csv;

    public Task ValidateAsync(DataSource dataSource)
    {
        if (string.IsNullOrWhiteSpace(dataSource.ConnectionString))
            throw new InvalidOperationException("CSV data source requires a valid file path.");
        return Task.CompletedTask;
    }
}