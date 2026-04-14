using Moq;
using WidgetData.Application.DTOs;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Services;
using WidgetData.Tests.TestData;

namespace WidgetData.Tests.Services;

public class WidgetConfigArchiveServiceTests
{
    private readonly Mock<IWidgetConfigArchiveRepository> _archiveRepoMock;
    private readonly Mock<IWidgetRepository> _widgetRepoMock;
    private readonly WidgetConfigArchiveService _service;

    public WidgetConfigArchiveServiceTests()
    {
        _archiveRepoMock = new Mock<IWidgetConfigArchiveRepository>();
        _widgetRepoMock = new Mock<IWidgetRepository>();
        _service = new WidgetConfigArchiveService(_archiveRepoMock.Object, _widgetRepoMock.Object);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static WidgetConfigArchive CreateArchive(int id, int widgetId,
        string? config = null, string? htmlTemplate = null) => new()
    {
        Id = id,
        WidgetId = widgetId,
        Configuration = config ?? "{\"query\":\"SELECT 1\"}",
        ChartConfig = "{\"type\":\"bar\"}",
        HtmlTemplate = htmlTemplate,
        Note = "snapshot",
        TriggerSource = "Manual",
        ArchivedBy = "user1",
        ArchivedAt = DateTime.UtcNow
    };

    // ─── GetByWidgetIdAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetByWidgetIdAsync_ReturnsAllArchivesForWidget()
    {
        var archives = new List<WidgetConfigArchive>
        {
            CreateArchive(1, 5),
            CreateArchive(2, 5)
        };
        _archiveRepoMock.Setup(r => r.GetByWidgetIdAsync(5)).ReturnsAsync(archives);
        _widgetRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(TestDataBuilder.CreateWidget(5));

        var result = (await _service.GetByWidgetIdAsync(5)).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, a => Assert.Equal(5, a.WidgetId));
    }

    [Fact]
    public async Task GetByWidgetIdAsync_IncludesWidgetNameFromRepo()
    {
        var archive = CreateArchive(1, 3);
        var widget = TestDataBuilder.CreateWidget(3);
        widget.Name = "Revenue Chart";
        _archiveRepoMock.Setup(r => r.GetByWidgetIdAsync(3)).ReturnsAsync(new[] { archive });
        _widgetRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(widget);

        var result = (await _service.GetByWidgetIdAsync(3)).ToList();

        Assert.Single(result);
        Assert.Equal("Revenue Chart", result[0].WidgetName);
    }

    [Fact]
    public async Task GetByWidgetIdAsync_WidgetNotFound_StillReturnsMappedArchives()
    {
        var archive = CreateArchive(1, 7);
        _archiveRepoMock.Setup(r => r.GetByWidgetIdAsync(7)).ReturnsAsync(new[] { archive });
        _widgetRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync((Widget?)null);

        var result = (await _service.GetByWidgetIdAsync(7)).ToList();

        Assert.Single(result);
        Assert.Null(result[0].WidgetName);
    }

    // ─── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_NonExistentWidget_ReturnsNull()
    {
        _widgetRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Widget?)null);

        var result = await _service.CreateAsync(99, new CreateWidgetConfigArchiveDto { Note = "x" }, "user1");

        Assert.Null(result);
        _archiveRepoMock.Verify(r => r.CreateAsync(It.IsAny<WidgetConfigArchive>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ValidWidget_CreatesArchiveWithWidgetConfig()
    {
        var widget = TestDataBuilder.CreateWidget(1);
        widget.Configuration = "{\"query\":\"SELECT NOW()\"}";
        widget.ChartConfig = "{\"type\":\"line\"}";
        widget.HtmlTemplate = "<p>{{val}}</p>";

        var dto = new CreateWidgetConfigArchiveDto { Note = "manual snapshot" };
        var created = CreateArchive(10, 1, widget.Configuration, widget.HtmlTemplate);
        created.Note = "manual snapshot";
        created.TriggerSource = "OnSave";

        _widgetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(widget);
        _archiveRepoMock.Setup(r => r.CreateAsync(It.IsAny<WidgetConfigArchive>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(1, dto, "user1", "OnSave");

        Assert.NotNull(result);
        Assert.Equal(1, result.WidgetId);
        Assert.Equal(widget.Configuration, result.Configuration);
        Assert.Equal(widget.HtmlTemplate, result.HtmlTemplate);
        _archiveRepoMock.Verify(r => r.CreateAsync(It.Is<WidgetConfigArchive>(a =>
            a.WidgetId == 1 &&
            a.Configuration == widget.Configuration &&
            a.ArchivedBy == "user1" &&
            a.TriggerSource == "OnSave")), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_PassesScheduleId()
    {
        var widget = TestDataBuilder.CreateWidget(2);
        var archive = CreateArchive(5, 2);
        archive.ScheduleId = 99;
        _widgetRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(widget);
        _archiveRepoMock.Setup(r => r.CreateAsync(It.IsAny<WidgetConfigArchive>())).ReturnsAsync(archive);

        await _service.CreateAsync(2, new CreateWidgetConfigArchiveDto(), "user1", "Schedule", scheduleId: 99);

        _archiveRepoMock.Verify(r => r.CreateAsync(It.Is<WidgetConfigArchive>(a =>
            a.ScheduleId == 99)), Times.Once);
    }

    // ─── RestoreAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RestoreAsync_NonExistentArchive_ReturnsNull()
    {
        _archiveRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((WidgetConfigArchive?)null);

        var result = await _service.RestoreAsync(1, 99, "user1");

        Assert.Null(result);
        _widgetRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Widget>()), Times.Never);
    }

    [Fact]
    public async Task RestoreAsync_ArchiveBelongsToDifferentWidget_ReturnsNull()
    {
        var archive = CreateArchive(5, widgetId: 2);
        _archiveRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(archive);

        // Request restore for widget 1, but archive belongs to widget 2
        var result = await _service.RestoreAsync(1, 5, "user1");

        Assert.Null(result);
        _widgetRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Widget>()), Times.Never);
    }

    [Fact]
    public async Task RestoreAsync_WidgetNotFound_ReturnsNull()
    {
        var archive = CreateArchive(5, widgetId: 1);
        _archiveRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(archive);
        _widgetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Widget?)null);

        var result = await _service.RestoreAsync(1, 5, "user1");

        Assert.Null(result);
    }

    [Fact]
    public async Task RestoreAsync_Valid_AutoArchivesCurrentConfigThenRestores()
    {
        const string oldConfig = "{\"query\":\"SELECT OLD\"}";
        const string restoredConfig = "{\"query\":\"SELECT RESTORED\"}";

        var archive = CreateArchive(5, widgetId: 1, config: restoredConfig);
        var widget = TestDataBuilder.CreateWidget(1);
        widget.Configuration = oldConfig;

        _archiveRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(archive);
        _widgetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(widget);
        _archiveRepoMock.Setup(r => r.CreateAsync(It.IsAny<WidgetConfigArchive>()))
            .ReturnsAsync((WidgetConfigArchive a) => a);
        _widgetRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Widget>()))
            .ReturnsAsync((Widget w) => { w.DataSource = TestDataBuilder.CreateDataSource(w.DataSourceId); return w; });

        var result = await _service.RestoreAsync(1, 5, "user1");

        Assert.NotNull(result);
        Assert.Equal(restoredConfig, result.Configuration);

        // Should auto-archive the current config before overwriting
        _archiveRepoMock.Verify(r => r.CreateAsync(It.Is<WidgetConfigArchive>(a =>
            a.Configuration == oldConfig && a.TriggerSource == "OnSave")), Times.Once);

        // Widget should be updated with restored config
        _widgetRepoMock.Verify(r => r.UpdateAsync(It.Is<Widget>(w =>
            w.Configuration == restoredConfig)), Times.Once);
    }

    // ─── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingArchive_ReturnsTrue()
    {
        _archiveRepoMock.Setup(r => r.DeleteAsync(3)).ReturnsAsync(true);

        var result = await _service.DeleteAsync(3);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentArchive_ReturnsFalse()
    {
        _archiveRepoMock.Setup(r => r.DeleteAsync(999)).ReturnsAsync(false);

        var result = await _service.DeleteAsync(999);

        Assert.False(result);
    }
}
