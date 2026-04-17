using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data;
using WidgetData.Infrastructure.Services;
using WidgetData.Tests.TestData;

namespace WidgetData.Tests.Services;

public class WidgetServiceTests
{
    private readonly Mock<IWidgetRepository> _widgetRepoMock;
    private readonly Mock<IExecutionRepository> _executionRepoMock;
    private readonly Mock<IWidgetConfigArchiveRepository> _archiveRepoMock;
    private readonly Mock<IScheduleRepository> _scheduleRepoMock;
    private readonly ApplicationDbContext _context;
    private readonly WidgetService _service;

    public WidgetServiceTests()
    {
        _widgetRepoMock = new Mock<IWidgetRepository>();
        _executionRepoMock = new Mock<IExecutionRepository>();
        _archiveRepoMock = new Mock<IWidgetConfigArchiveRepository>();
        _scheduleRepoMock = new Mock<IScheduleRepository>();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        var auditServiceMock = new Mock<IAuditService>();
        var loggerMock = new Mock<ILogger<WidgetService>>();
        _service = new WidgetService(_widgetRepoMock.Object, _executionRepoMock.Object, _context,
            _archiveRepoMock.Object, _scheduleRepoMock.Object, auditServiceMock.Object, loggerMock.Object);
    }

    // ─── GetAllAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllWidgets()
    {
        var widgets = new List<Widget>
        {
            TestDataBuilder.CreateWidget(1),
            TestDataBuilder.CreateWidget(2)
        };
        _widgetRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(widgets);

        var result = await _service.GetAllAsync();

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_EmptyList_ReturnsEmpty()
    {
        _widgetRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Widget>());

        var result = await _service.GetAllAsync();

        Assert.Empty(result);
    }

    // ─── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var widget = TestDataBuilder.CreateWidget(1);
        _widgetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(widget);

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(widget.Id, result.Id);
        Assert.Equal(widget.Name, result.Name);
        Assert.Equal(widget.WidgetType, result.WidgetType);
        Assert.Equal(widget.DataSourceId, result.DataSourceId);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        _widgetRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Widget?)null);

        var result = await _service.GetByIdAsync(99);

        Assert.Null(result);
    }

    // ─── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedWidget()
    {
        var dto = TestDataBuilder.CreateWidgetDto("My Widget");
        var created = new Widget
        {
            Id = 10,
            Name = dto.Name,
            WidgetType = dto.WidgetType,
            DataSourceId = dto.DataSourceId,
            CacheEnabled = dto.CacheEnabled,
            CacheTtlMinutes = dto.CacheTtlMinutes,
            CreatedBy = "user1",
            DataSource = TestDataBuilder.CreateDataSource(dto.DataSourceId)
        };
        _widgetRepoMock.Setup(r => r.CreateAsync(It.IsAny<Widget>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(dto, "user1");

        Assert.Equal(10, result.Id);
        Assert.Equal("My Widget", result.Name);
        Assert.Equal(dto.WidgetType, result.WidgetType);
        Assert.Equal("user1", result.CreatedBy);
        _widgetRepoMock.Verify(r => r.CreateAsync(It.Is<Widget>(w =>
            w.Name == dto.Name &&
            w.CreatedBy == "user1" &&
            w.DataSourceId == dto.DataSourceId)), Times.Once);
    }

    // ─── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingId_ReturnsUpdatedDto()
    {
        var existing = TestDataBuilder.CreateWidget(1);
        var dto = TestDataBuilder.UpdateWidgetDto("Updated");
        var updated = new Widget
        {
            Id = 1,
            Name = dto.Name,
            WidgetType = dto.WidgetType,
            DataSourceId = dto.DataSourceId,
            IsActive = dto.IsActive,
            DataSource = TestDataBuilder.CreateDataSource(dto.DataSourceId)
        };
        _widgetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _widgetRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Widget>())).ReturnsAsync(updated);
        _archiveRepoMock.Setup(r => r.CreateAsync(It.IsAny<WidgetConfigArchive>()))
            .ReturnsAsync((WidgetConfigArchive a) => a);

        var result = await _service.UpdateAsync(1, dto);

        Assert.NotNull(result);
        Assert.Equal("Updated", result.Name);
        _widgetRepoMock.Verify(r => r.UpdateAsync(It.Is<Widget>(w =>
            w.Name == "Updated" && w.UpdatedAt != null)), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentId_ReturnsNull()
    {
        _widgetRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Widget?)null);

        var result = await _service.UpdateAsync(99, TestDataBuilder.UpdateWidgetDto());

        Assert.Null(result);
        _widgetRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Widget>()), Times.Never);
    }

    // ─── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsTrue()
    {
        var widget = TestDataBuilder.CreateWidget(1);
        _widgetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(widget);
        _widgetRepoMock.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(1);

        Assert.True(result);
        _widgetRepoMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_ReturnsFalse()
    {
        _widgetRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Widget?)null);

        var result = await _service.DeleteAsync(99);

        Assert.False(result);
        _widgetRepoMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    // ─── ExecuteAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ExistingWidget_ReturnsSuccessExecution()
    {
        var widget = TestDataBuilder.CreateWidget(1);
        var runningExecution = TestDataBuilder.CreateRunningExecution(10, 1);
        var completedExecution = new WidgetExecution
        {
            Id = 10,
            WidgetId = 1,
            Status = ExecutionStatus.Success,
            TriggeredBy = ExecutionTrigger.Manual,
            UserId = "user1",
            RowCount = 0,
            ExecutionTimeMs = 100,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        _widgetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(widget);
        _executionRepoMock.Setup(r => r.CreateAsync(It.IsAny<WidgetExecution>())).ReturnsAsync(runningExecution);
        _executionRepoMock.Setup(r => r.UpdateAsync(It.IsAny<WidgetExecution>())).ReturnsAsync(completedExecution);
        _widgetRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Widget>())).ReturnsAsync(widget);

        var result = await _service.ExecuteAsync(1, "user1");

        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Equal(1, result.WidgetId);
        _executionRepoMock.Verify(r => r.CreateAsync(It.Is<WidgetExecution>(e =>
            e.WidgetId == 1 &&
            e.TriggeredBy == ExecutionTrigger.Manual &&
            e.UserId == "user1")), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_NonExistentWidget_ThrowsKeyNotFoundException()
    {
        _widgetRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Widget?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.ExecuteAsync(99, "user1"));
    }

    // ─── GetHistoryAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetHistoryAsync_ReturnsExecutionsForWidget()
    {
        var executions = new List<WidgetExecution>
        {
            TestDataBuilder.CreateExecution(1, 1, ExecutionStatus.Success),
            TestDataBuilder.CreateExecution(2, 1, ExecutionStatus.Failed)
        };
        _executionRepoMock.Setup(r => r.GetByWidgetIdAsync(1)).ReturnsAsync(executions);

        var result = (await _service.GetHistoryAsync(1)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Status == ExecutionStatus.Success);
        Assert.Contains(result, e => e.Status == ExecutionStatus.Failed);
    }

    [Fact]
    public async Task GetHistoryAsync_NoExecutions_ReturnsEmpty()
    {
        _executionRepoMock.Setup(r => r.GetByWidgetIdAsync(5)).ReturnsAsync(new List<WidgetExecution>());

        var result = await _service.GetHistoryAsync(5);

        Assert.Empty(result);
    }

    // ─── GetDataAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDataAsync_ExistingWidget_ReturnsObject()
    {
        _widgetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataBuilder.CreateWidget(1));

        var result = await _service.GetDataAsync(1);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetDataAsync_NonExistentWidget_ReturnsNull()
    {
        _widgetRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Widget?)null);

        var result = await _service.GetDataAsync(99);

        Assert.Null(result);
    }

    // ─── Mapping ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_MapsDataSourceNameCorrectly()
    {
        var widget = TestDataBuilder.CreateWidget(1, 1);
        widget.DataSource.Name = "Production DB";
        _widgetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(widget);

        var result = await _service.GetByIdAsync(1);

        Assert.Equal("Production DB", result!.DataSourceName);
    }

    // ─── HtmlTemplate field ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithHtmlTemplate_SetsHtmlTemplateOnEntity()
    {
        const string template = "<p>{{name}}</p>";
        var dto = TestDataBuilder.CreateWidgetDto("HTML Widget");
        dto.HtmlTemplate = template;

        var created = new Widget
        {
            Id = 20,
            Name = dto.Name,
            WidgetType = dto.WidgetType,
            DataSourceId = dto.DataSourceId,
            HtmlTemplate = template,
            CreatedBy = "user1",
            DataSource = TestDataBuilder.CreateDataSource(dto.DataSourceId)
        };
        _widgetRepoMock.Setup(r => r.CreateAsync(It.IsAny<Widget>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(dto, "user1");

        Assert.Equal(template, result.HtmlTemplate);
        _widgetRepoMock.Verify(r => r.CreateAsync(It.Is<Widget>(w => w.HtmlTemplate == template)), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithHtmlTemplate_UpdatesHtmlTemplateOnEntity()
    {
        const string updatedTemplate = "<table>{{#each rows}}<tr><td>{{name}}</td></tr>{{/each}}</table>";
        var existing = TestDataBuilder.CreateWidget(1);
        var dto = TestDataBuilder.UpdateWidgetDto("HTML Widget Updated");
        dto.HtmlTemplate = updatedTemplate;

        var updated = new Widget
        {
            Id = 1,
            Name = dto.Name,
            WidgetType = dto.WidgetType,
            DataSourceId = dto.DataSourceId,
            HtmlTemplate = updatedTemplate,
            DataSource = TestDataBuilder.CreateDataSource(dto.DataSourceId)
        };
        _widgetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _widgetRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Widget>())).ReturnsAsync(updated);
        _archiveRepoMock.Setup(r => r.CreateAsync(It.IsAny<WidgetConfigArchive>()))
            .ReturnsAsync((WidgetConfigArchive a) => a);

        var result = await _service.UpdateAsync(1, dto);

        Assert.Equal(updatedTemplate, result!.HtmlTemplate);
        _widgetRepoMock.Verify(r => r.UpdateAsync(It.Is<Widget>(w => w.HtmlTemplate == updatedTemplate)), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_MapsHtmlTemplateCorrectly()
    {
        const string template = "<div>{{value}}</div>";
        var widget = TestDataBuilder.CreateWidget(1);
        widget.HtmlTemplate = template;
        _widgetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(widget);

        var result = await _service.GetByIdAsync(1);

        Assert.Equal(template, result!.HtmlTemplate);
    }

    [Fact]
    public async Task GetByIdAsync_NullHtmlTemplate_MapsAsNull()
    {
        var widget = TestDataBuilder.CreateWidget(1);
        widget.HtmlTemplate = null;
        _widgetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(widget);

        var result = await _service.GetByIdAsync(1);

        Assert.Null(result!.HtmlTemplate);
    }
}
