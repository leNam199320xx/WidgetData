using Moq;
using Microsoft.Extensions.Logging;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;
using WidgetData.DataSources;
using WidgetData.Tests.TestData;

namespace WidgetData.Tests.Services;

public class DataSourceCrudServiceTests
{
    private readonly Mock<IDataSourceRepository> _repoMock;
    private readonly Mock<ILogger<DataSourceCrudService>> _loggerMock;
    private readonly DataSourceCrudService _service;

    public DataSourceCrudServiceTests()
    {
        _repoMock = new Mock<IDataSourceRepository>();
        _loggerMock = new Mock<ILogger<DataSourceCrudService>>();
        _service = new DataSourceCrudService(_repoMock.Object, Enumerable.Empty<WidgetData.Application.Interfaces.IDataSourceValidator>(), _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var dataSources = new List<DataSource>
        {
            TestDataBuilder.CreateDataSource(1, "DB1"),
            TestDataBuilder.CreateDataSource(2, "DB2")
        };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(dataSources);

        var result = (await _service.GetAllAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("DB1", result[0].Name);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var ds = TestDataBuilder.CreateDataSource(1, "My DB");
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ds);

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("My DB", result.Name);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        var dto = TestDataBuilder.CreateDataSourceDto("New DS");
        var created = new DataSource { Id = 5, Name = dto.Name, SourceType = dto.SourceType, CreatedBy = "user1", CreatedAt = DateTime.UtcNow };
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<DataSource>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(dto, "user1");

        Assert.Equal(5, result.Id);
        Assert.Equal("New DS", result.Name);
        _repoMock.Verify(r => r.CreateAsync(It.Is<DataSource>(d => d.CreatedBy == "user1")), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingId_ReturnsUpdatedDto()
    {
        var existing = TestDataBuilder.CreateDataSource(1);
        var dto = TestDataBuilder.UpdateDataSourceDto("Updated DS");
        var updated = new DataSource { Id = 1, Name = dto.Name, UpdatedAt = DateTime.UtcNow, CreatedAt = existing.CreatedAt };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<DataSource>())).ReturnsAsync(updated);

        var result = await _service.UpdateAsync(1, dto);

        Assert.NotNull(result);
        Assert.Equal("Updated DS", result.Name);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsTrue()
    {
        var ds = TestDataBuilder.CreateDataSource(1);
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ds);
        _repoMock.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(1);

        Assert.True(result);
    }
}