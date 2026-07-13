using Moq;
using Microsoft.Extensions.Logging;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;
using WidgetData.Pages;
using WidgetData.Tests.TestData;

namespace WidgetData.Tests.Services;

public class PageCrudServiceTests
{
    private readonly Mock<IPageRepository> _repoMock;
    private readonly Mock<IPageVersioningService> _versioningMock;
    private readonly Mock<ILogger<PageCrudService>> _loggerMock;
    private readonly PageCrudService _service;

    public PageCrudServiceTests()
    {
        _repoMock = new Mock<IPageRepository>();
        _versioningMock = new Mock<IPageVersioningService>();
        _loggerMock = new Mock<ILogger<PageCrudService>>();
        _service = new PageCrudService(_repoMock.Object, _versioningMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedPage()
    {
        var dto = TestDataBuilder.CreatePageDto("New Page");
        var created = new Page { Id = 1, Title = dto.Title, Slug = dto.Slug, TenantId = 1, CurrentVersion = 1, CreatedBy = "user1", CreatedAt = DateTime.UtcNow };
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Page>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(dto, 1, "user1");

        Assert.Equal(1, result.Id);
        Assert.Equal("New Page", result.Title);
        _versioningMock.Verify(v => v.SaveSnapshotAsync(It.IsAny<Page>(), "user1", "DraftSaved", "Initial draft created"), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingId_ReturnsUpdatedPage()
    {
        var existing = TestDataBuilder.CreatePage(1);
        var dto = TestDataBuilder.UpdatePageDto("Updated Page");
        var updated = new Page { Id = 1, Title = dto.Title, Slug = dto.Slug, TenantId = 1, CurrentVersion = 2, CreatedBy = "user1", CreatedAt = DateTime.UtcNow };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Page>())).ReturnsAsync(updated);

        var result = await _service.UpdateAsync(1, dto);

        Assert.NotNull(result);
        Assert.Equal("Updated Page", result.Title);
        _versioningMock.Verify(v => v.SaveSnapshotAsync(It.IsAny<Page>(), It.IsAny<string>(), "DraftSaved", "Draft updated"), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsTrue()
    {
        var page = TestDataBuilder.CreatePage(1);
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(page);
        _repoMock.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(1);

        Assert.True(result);
    }

    [Fact]
    public async Task CanResolvePageLayoutService()
    {
        var layoutService = new WidgetData.Pages.PageLayoutService(_repoMock.Object, _loggerMock.Object);
        Assert.NotNull(layoutService);
    }
}