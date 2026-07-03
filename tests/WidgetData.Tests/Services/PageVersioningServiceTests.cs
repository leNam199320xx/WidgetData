using Moq;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Services;
using WidgetData.Tests.TestData;

namespace WidgetData.Tests.Services;

public class PageVersioningServiceTests
{
    private readonly Mock<IPageRepository> _repoMock;
    private readonly Mock<ILogger<PageVersioningService>> _loggerMock;
    private readonly PageVersioningService _service;

    public PageVersioningServiceTests()
    {
        _repoMock = new Mock<IPageRepository>();
        _loggerMock = new Mock<ILogger<PageVersioningService>>();
        _service = new PageVersioningService(_repoMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task PublishAsync_ExistingPage_ReturnsPublishedPage()
    {
        var page = TestDataBuilder.CreatePage(1);
        var published = new Page { Id = 1, Title = page.Title, LifecycleState = ScreenLifecycleState.Published, CurrentVersion = 2, TenantId = 1 };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(page);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Page>())).ReturnsAsync(published);

        var result = await _service.PublishAsync(1, "user1");

        Assert.NotNull(result);
        Assert.Equal(ScreenLifecycleState.Published, result.LifecycleState);
        _repoMock.Verify(r => r.CreateVersionAsync(It.IsAny<PageVersion>()), Times.Once);
    }

    [Fact]
    public async Task RollbackAsync_ExistingPageAndVersion_ReturnsRolledBackPage()
    {
        var page = TestDataBuilder.CreatePage(1);
        var snapshot = new PageVersioningService.PageSnapshot { Title = "Old Title", Slug = "old-slug", ScreenType = ScreenType.Frontend, LifecycleState = ScreenLifecycleState.Draft, IsActive = true, Widgets = new List<PageVersioningService.PageWidgetSnapshot>() };
        var version = new PageVersion { Id = 1, PageId = 1, VersionNumber = 1, SnapshotJson = JsonSerializer.Serialize(snapshot) };
        var rolledBack = new Page { Id = 1, Title = "Old Title", Slug = "old-slug", CurrentVersion = 2, TenantId = 1 };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(page);
        _repoMock.Setup(r => r.GetVersionAsync(1, 1)).ReturnsAsync(version);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Page>())).ReturnsAsync(rolledBack);

        var result = await _service.RollbackAsync(1, 1, "user1");

        Assert.NotNull(result);
        Assert.Equal("Old Title", result.Title);
        _repoMock.Verify(r => r.CreateVersionAsync(It.IsAny<PageVersion>()), Times.Once);
    }
}