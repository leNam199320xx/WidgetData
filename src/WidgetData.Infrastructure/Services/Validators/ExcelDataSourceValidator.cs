using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;

namespace WidgetData.Infrastructure.Services;

public class ExcelDataSourceValidator : IDataSourceValidator
{
    public bool CanValidate(DataSourceType sourceType) => sourceType == DataSourceType.Excel;

    public Task ValidateAsync(DataSource dataSource)
    {
        if (string.IsNullOrWhiteSpace(dataSource.ConnectionString))
            throw new InvalidOperationException("Excel data source requires a valid file path.");
        return Task.CompletedTask;
    }
}