using WidgetData.Domain.Enums;

namespace WidgetData.Application.Interfaces;

public interface IFileHandler
{
    bool IsFileSourceType(DataSourceType sourceType);
    string[] GetAllowedExtensions(DataSourceType sourceType);
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType, string folder);
}
