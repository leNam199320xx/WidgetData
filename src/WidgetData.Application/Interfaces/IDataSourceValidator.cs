using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;

namespace WidgetData.Application.Interfaces;

public interface IDataSourceValidator
{
    bool CanValidate(DataSourceType sourceType);
    Task ValidateAsync(DataSource dataSource);
}
