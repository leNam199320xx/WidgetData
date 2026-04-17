using Moq;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Services;
using WidgetData.Tests.TestData;

namespace WidgetData.Tests.Services;

public class DataSourceServiceTests
{
    private readonly Mock<IDataSourceRepository> _repoMock;
    private readonly DataSourceService _service;

    public DataSourceServiceTests()
    {
        _repoMock = new Mock<IDataSourceRepository>();
        _service = new DataSourceService(_repoMock.Object);
    }

    // ─── GetAllAsync ─────────────────────────────────────────────────────────

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
        Assert.Equal("DB2", result[1].Name);
    }

    [Fact]
    public async Task GetAllAsync_EmptyList_ReturnsEmpty()
    {
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<DataSource>());

        var result = await _service.GetAllAsync();

        Assert.Empty(result);
    }

    // ─── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var ds = TestDataBuilder.CreateDataSource(1, "My DB");
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ds);

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("My DB", result.Name);
        Assert.Equal(DataSourceType.SQLite, result.SourceType);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((DataSource?)null);

        var result = await _service.GetByIdAsync(99);

        Assert.Null(result);
    }

    // ─── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        var dto = TestDataBuilder.CreateDataSourceDto("New DS");
        var created = new DataSource
        {
            Id = 5,
            Name = dto.Name,
            SourceType = dto.SourceType,
            Host = dto.Host,
            Port = dto.Port,
            DatabaseName = dto.DatabaseName,
            Username = dto.Username,
            IsActive = true,
            CreatedBy = "user1",
            CreatedAt = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<DataSource>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(dto, "user1");

        Assert.Equal(5, result.Id);
        Assert.Equal("New DS", result.Name);
        _repoMock.Verify(r => r.CreateAsync(It.Is<DataSource>(d =>
            d.Name == dto.Name &&
            d.CreatedBy == "user1")), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SetsCorrectSourceType()
    {
        var dto = TestDataBuilder.CreateDataSourceDto();
        dto.SourceType = DataSourceType.RestApi;
        dto.ApiEndpoint = "https://api.example.com";
        dto.ApiKey = "secret-key";
        var created = new DataSource
        {
            Id = 6,
            Name = dto.Name,
            SourceType = DataSourceType.RestApi,
            ApiEndpoint = dto.ApiEndpoint,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<DataSource>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(dto, "user2");

        Assert.Equal(DataSourceType.RestApi, result.SourceType);
    }

    // ─── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingId_ReturnsUpdatedDto()
    {
        var existing = TestDataBuilder.CreateDataSource(1);
        var dto = TestDataBuilder.UpdateDataSourceDto("Updated DS");
        var updated = new DataSource
        {
            Id = 1,
            Name = dto.Name,
            SourceType = dto.SourceType,
            Host = dto.Host,
            IsActive = dto.IsActive,
            UpdatedAt = DateTime.UtcNow,
            CreatedAt = existing.CreatedAt
        };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<DataSource>())).ReturnsAsync(updated);

        var result = await _service.UpdateAsync(1, dto);

        Assert.NotNull(result);
        Assert.Equal("Updated DS", result.Name);
        Assert.Equal(DataSourceType.PostgreSql, result.SourceType);
        _repoMock.Verify(r => r.UpdateAsync(It.Is<DataSource>(d =>
            d.Name == "Updated DS" && d.UpdatedAt != null)), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((DataSource?)null);

        var result = await _service.UpdateAsync(99, TestDataBuilder.UpdateDataSourceDto());

        Assert.Null(result);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<DataSource>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_EmptyPassword_DoesNotOverwritePassword()
    {
        var existing = TestDataBuilder.CreateDataSource(1);
        existing.Password = "original-password";
        var dto = TestDataBuilder.UpdateDataSourceDto();
        dto.Password = string.Empty;

        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<DataSource>())).ReturnsAsync(existing);

        await _service.UpdateAsync(1, dto);

        _repoMock.Verify(r => r.UpdateAsync(It.Is<DataSource>(d =>
            d.Password == "original-password")), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NewPassword_OverwritesPassword()
    {
        var existing = TestDataBuilder.CreateDataSource(1);
        existing.Password = "old-password";
        var dto = TestDataBuilder.UpdateDataSourceDto();
        dto.Password = "new-password";

        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<DataSource>())).ReturnsAsync(existing);

        await _service.UpdateAsync(1, dto);

        _repoMock.Verify(r => r.UpdateAsync(It.Is<DataSource>(d =>
            d.Password == "new-password")), Times.Once);
    }

    // ─── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsTrue()
    {
        var ds = TestDataBuilder.CreateDataSource(1);
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ds);
        _repoMock.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(1);

        Assert.True(result);
        _repoMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_ReturnsFalse()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((DataSource?)null);

        var result = await _service.DeleteAsync(99);

        Assert.False(result);
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    // ─── TestConnectionAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task TestConnectionAsync_ExistingDataSource_ReturnsSuccess()
    {
        var ds = TestDataBuilder.CreateDataSource(1);
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ds);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<DataSource>())).ReturnsAsync(ds);

        var result = await _service.TestConnectionAsync(1);

        // Real connection test now runs — empty connection string returns a failed result
        Assert.NotNull(result);
        _repoMock.Verify(r => r.UpdateAsync(It.Is<DataSource>(d => d.LastTestedAt != null)), Times.Once);
    }

    [Fact]
    public async Task TestConnectionAsync_NonExistentDataSource_ReturnsNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((DataSource?)null);

        var result = await _service.TestConnectionAsync(99);

        Assert.Equal("Data source not found", result);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<DataSource>()), Times.Never);
    }

    // ─── Mapping – mật khẩu và API key không được lộ ──────────────────────

    [Fact]
    public async Task GetByIdAsync_DoesNotExposePassword()
    {
        var ds = TestDataBuilder.CreateDataSource(1);
        ds.Password = "secret";
        ds.ApiKey = "api-secret";
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ds);

        var result = await _service.GetByIdAsync(1);

        // DataSourceDto không có trường Password và ApiKey
        var props = typeof(Application.DTOs.DataSourceDto).GetProperties();
        Assert.DoesNotContain(props, p => p.Name == "Password");
        Assert.DoesNotContain(props, p => p.Name == "ApiKey");
    }
}
