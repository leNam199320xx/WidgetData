using Moq;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;
using WidgetData.DataSources;
using WidgetData.Tests.TestData;

namespace WidgetData.Tests.Services;

public class DataSourceConnectivityTestServiceTests
{
    private readonly Mock<IDataSourceRepository> _repoMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<DataSourceConnectivityTestService>> _loggerMock;
    private readonly DataSourceConnectivityTestService _service;

    public DataSourceConnectivityTestServiceTests()
    {
        _repoMock = new Mock<IDataSourceRepository>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<DataSourceConnectivityTestService>>();
        _service = new DataSourceConnectivityTestService(_repoMock.Object, _httpClientFactoryMock.Object,
            Enumerable.Empty<WidgetData.Application.Interfaces.IDataSourceValidator>(), _loggerMock.Object);
    }

    [Fact]
    public async Task TestConnectionAsync_NonExistentDataSource_ReturnsNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((DataSource?)null);

        var result = await _service.TestConnectionAsync(99);

        Assert.Equal("Data source not found", result);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<DataSource>()), Times.Never);
    }

    [Fact]
    public async Task TestConnectionAsync_ExistingDataSource_UpdatesLastTested()
    {
        var ds = TestDataBuilder.CreateDataSource(1);
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ds);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<DataSource>())).ReturnsAsync(ds);

        var result = await _service.TestConnectionAsync(1);

        Assert.NotNull(result);
        _repoMock.Verify(r => r.UpdateAsync(It.Is<DataSource>(d => d.LastTestedAt != null)), Times.Once);
    }
}