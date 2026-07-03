using WidgetData.Application.DTOs;
using WidgetData.Domain.Enums;

namespace WidgetData.Application.Interfaces;

public interface IDataSourceUploadService
{
    Task<DataSourceFileUploadDto?> UploadFileAsync(
        int id,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSizeBytes,
        string uploadedBy);
}