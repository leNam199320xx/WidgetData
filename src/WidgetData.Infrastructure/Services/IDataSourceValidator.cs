using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;

namespace WidgetData.Infrastructure.Services;

public interface IDataSourceValidator
{
    bool CanValidate(DataSourceType sourceType);
    Task ValidateAsync(DataSource dataSource);
}