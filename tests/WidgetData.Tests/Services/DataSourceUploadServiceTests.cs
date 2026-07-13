using Moq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;
using WidgetData.DataSources;
using WidgetData.Tests.TestData;

namespace WidgetData.Tests.Services;

public class DataSourceUploadServiceTests
{
    private readonly Mock<IDataSourceRepository> _repoMock;
    private readonly Mock<WidgetData.Application.Interfaces.IFileHandler> _fileHandlerMock;
    private readonly Mock<IHostEnvironment> _hostEnvMock;
    private readonly Mock<ILogger<DataSourceUploadService>> _loggerMock;
    private readonly DataSourceUploadService _service;

    public DataSourceUploadServiceTests()
    {
        _repoMock = new Mock<IDataSourceRepository>();
        _fileHandlerMock = new Mock<WidgetData.Application.Interfaces.IFileHandler>();
        _hostEnvMock = new Mock<IHostEnvironment>();
        _loggerMock = new Mock<ILogger<DataSourceUploadService>>();
        _service = new DataSourceUploadService(_repoMock.Object, _fileHandlerMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task UploadFileAsync_NonExistentDataSource_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((DataSource?)null);

        var result = await _service.UploadFileAsync(99, Stream.Null, "test.csv", "text/csv", 100, "user1");

        Assert.Null(result);
    }

    [Fact]
    public async Task UploadFileAsync_ValidFile_ReturnsUploadResult()
    {
        var ds = TestDataBuilder.CreateDataSource(1);
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ds);
        _fileHandlerMock.Setup(f => f.IsFileSourceType(It.IsAny<DataSourceType>())).Returns(true);
        _fileHandlerMock.Setup(f => f.GetAllowedExtensions(It.IsAny<DataSourceType>())).Returns(new[] { ".csv" });
        _fileHandlerMock.Setup(f => f.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("C:\\uploads\\test.csv");

        using var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        var result = await _service.UploadFileAsync(1, ms, "test.csv", "text/csv", 3, "user1");

        Assert.NotNull(result);
        Assert.Equal("test.csv", result.OriginalFileName);
        _repoMock.Verify(r => r.UpdateAsync(It.Is<DataSource>(d => d.FileStoragePath == "C:\\uploads\\test.csv")), Times.Once);
    }
}