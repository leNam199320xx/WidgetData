using System.IO;
using Microsoft.Extensions.Logging;
using WidgetData.Application.Interfaces;
using WidgetData.Application.DTOs;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;

namespace WidgetData.DataSources;

public class DataSourceUploadService : IDataSourceUploadService
{
    private readonly IDataSourceRepository _repo;
    private readonly IFileHandler _fileHandler;
    private readonly ILogger _logger;
    private readonly ITenantContext? _tenantContext;

    public DataSourceUploadService(IDataSourceRepository repo, IFileHandler fileHandler,
        ILogger logger, ITenantContext? tenantContext = null)
    {
        _repo = repo;
        _fileHandler = fileHandler;
        _logger = logger;
        _tenantContext = tenantContext;
    }

    public async Task<DataSourceFileUploadDto?> UploadFileAsync(
        int id,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSizeBytes,
        string uploadedBy)
    {
        var ds = await _repo.GetByIdAsync(id);
        if (ds == null) return null;

        if (!CanManage(ds))
            throw new UnauthorizedAccessException("Forbidden to upload file for this data source.");

        if (!_fileHandler.IsFileSourceType(ds.SourceType))
            throw new InvalidOperationException("Only CSV/Excel/Json data sources support file upload.");

        if (fileSizeBytes <= 0 || fileSizeBytes > 20 * 1024 * 1024)
            throw new InvalidOperationException("Invalid file size. Maximum allowed size is 20MB.");

        var allowed = _fileHandler.GetAllowedExtensions(ds.SourceType);
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            throw new InvalidOperationException($"Invalid file type for {ds.SourceType}. Allowed: {string.Join(", ", allowed)}");

        var tenantFolder = ds.TenantId?.ToString() ?? "shared";
        var root = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "datasources", tenantFolder, ds.Id.ToString());
        Directory.CreateDirectory(root);

        var fullPath = await _fileHandler.SaveFileAsync(fileStream, fileName, contentType, root);

        ds.OriginalFileName = Path.GetFileName(fileName);
        ds.StoredFileName = Path.GetFileName(fullPath);
        ds.FileContentType = contentType;
        ds.FileSizeBytes = fileSizeBytes;
        ds.FileStoragePath = fullPath;
        ds.FileUploadedAt = DateTime.UtcNow;
        ds.FileUploadedBy = uploadedBy;
        ds.ConnectionString = fullPath;
        ds.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(ds);

        return new DataSourceFileUploadDto
        {
            OriginalFileName = ds.OriginalFileName,
            StoredFileName = ds.StoredFileName,
            ContentType = ds.FileContentType ?? "application/octet-stream",
            FileSizeBytes = ds.FileSizeBytes ?? 0,
            UploadedAt = ds.FileUploadedAt ?? DateTime.UtcNow
        };
    }

    private bool CanManage(DataSource ds)
    {
        if (_tenantContext?.IsSuperAdmin == true) return true;
        if (_tenantContext?.CurrentTenantId == null) return true;
        return ds.TenantId == null || ds.TenantId == _tenantContext.CurrentTenantId;
    }
}
