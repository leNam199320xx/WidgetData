using Microsoft.Extensions.Hosting;
using WidgetData.Domain.Enums;

namespace WidgetData.Infrastructure.Services;

public class FileHandler : IFileHandler
{
    private readonly IHostEnvironment _hostEnvironment;

    public FileHandler(IHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
    }

    public bool IsFileSourceType(DataSourceType sourceType)
        => sourceType is DataSourceType.Csv or DataSourceType.Excel or DataSourceType.Json;

    public string[] GetAllowedExtensions(DataSourceType sourceType) => sourceType switch
    {
        DataSourceType.Csv => new[] { ".csv" },
        DataSourceType.Excel => new[] { ".xlsx", ".xls" },
        DataSourceType.Json => new[] { ".json" },
        _ => Array.Empty<string>()
    };

    public Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType, string folder)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(folder, storedFileName);

        using (var target = File.Create(fullPath))
        {
            fileStream.CopyTo(target);
        }

        return Task.FromResult(fullPath);
    }
}