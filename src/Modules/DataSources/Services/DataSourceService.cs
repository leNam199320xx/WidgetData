using System.IO;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WidgetData.Application.Interfaces;
using WidgetData.Application.DTOs;
using WidgetData.Domain.Interfaces;

namespace WidgetData.DataSources;

public class DataSourceService : IDataSourceService
{
    private readonly IDataSourceCrudService _crud;
    private readonly IDataSourceUploadService _upload;
    private readonly IDataSourceConnectivityTestService _test;

    public DataSourceService(IDataSourceRepository repo, IHttpClientFactory httpClientFactory,
        IHostEnvironment hostEnvironment, ITenantContext? tenantContext = null)
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var crudLogger = loggerFactory.CreateLogger<DataSourceCrudService>();
        var uploadLogger = loggerFactory.CreateLogger<DataSourceUploadService>();
        var testLogger = loggerFactory.CreateLogger<DataSourceConnectivityTestService>();

        _crud = new DataSourceCrudService(repo, Enumerable.Empty<IDataSourceValidator>(), crudLogger, tenantContext);
        _upload = new DataSourceUploadService(repo, new FileHandler(hostEnvironment), uploadLogger, tenantContext);
        _test = new DataSourceConnectivityTestService(repo, httpClientFactory, Enumerable.Empty<IDataSourceValidator>(), testLogger);
    }

    public Task<IEnumerable<DataSourceDto>> GetAllAsync() => _crud.GetAllAsync();
    public Task<DataSourceDto?> GetByIdAsync(int id) => _crud.GetByIdAsync(id);
    public Task<DataSourceDto> CreateAsync(CreateDataSourceDto dto, string userId) => _crud.CreateAsync(dto, userId);
    public Task<DataSourceDto?> UpdateAsync(int id, UpdateDataSourceDto dto) => _crud.UpdateAsync(id, dto);
    public Task<DataSourceFileUploadDto?> UploadFileAsync(int id, Stream fileStream, string fileName, string contentType, long fileSizeBytes, string uploadedBy)
        => _upload.UploadFileAsync(id, fileStream, fileName, contentType, fileSizeBytes, uploadedBy);
    public Task<bool> DeleteAsync(int id) => _crud.DeleteAsync(id);
    public Task<string> TestConnectionAsync(int id) => _test.TestConnectionAsync(id);
}
