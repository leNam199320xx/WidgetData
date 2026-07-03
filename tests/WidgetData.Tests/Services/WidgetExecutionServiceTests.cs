using Microsoft.Extensions.Logging;
using Moq;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Repositories;
using WidgetData.Infrastructure.Services;
using WidgetData.Tests.TestData;

namespace WidgetData.Tests.Services;

public class WidgetExecutionServiceTests
{
    private readonly Mock<IWidgetRepository> _widgetRepoMock;
    private readonly Mock<IExecutionRepository> _executionRepoMock;
    private readonly Mock<IWidgetConfigArchiveRepository> _archiveRepoMock;
    private readonly Mock<IScheduleRepository> _scheduleRepoMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ILogger<WidgetExecutionService>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IDataSourceStrategy> _strategyMock;
    private readonly WidgetExecutionService _service;

    public WidgetExecutionServiceTests()
    {
        _widgetRepoMock = new Mock<IWidgetRepository>();
        _executionRepoMock = new Mock<IExecutionRepository>();
        _archiveRepoMock = new Mock<IWidgetConfigArchiveRepository>();
        _scheduleRepoMock = new Mock<IScheduleRepository>();
        _auditServiceMock = new Mock<IAuditService>();
        _loggerMock = new Mock<ILogger<WidgetExecutionService>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _strategyMock = new Mock<IDataSourceStrategy>();

        _service = new WidgetExecutionService(_widgetRepoMock.Object, _executionRepoMock.Object,
            _archiveRepoMock.Object, _scheduleRepoMock.Object, _auditServiceMock.Object,
            _loggerMock.Object, _httpClientFactoryMock.Object,
            new IDataSourceStrategy[] { _strategyMock.Object });
    }

    [Fact]
    public async Task ExecuteAsync_WithSchedule_ArchivesConfigWhenArchiveConfigOnRunIsTrue()
    {
        var widget = TestDataBuilder.CreateWidget(1);
        var schedule = new WidgetSchedule { Id = 1, ArchiveConfigOnRun = true };
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
        _scheduleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(schedule);
        _executionRepoMock.Setup(r => r.CreateAsync(It.IsAny<WidgetExecution>())).ReturnsAsync(runningExecution);
        _executionRepoMock.Setup(r => r.UpdateAsync(It.IsAny<WidgetExecution>())).ReturnsAsync(completedExecution);
        _widgetRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Widget>())).ReturnsAsync(widget);

        var result = await _service.ExecuteAsync(1, "user1", scheduleId: 1);

        Assert.Equal(ExecutionStatus.Success, result.Status);
        _archiveRepoMock.Verify(r => r.CreateAsync(It.Is<WidgetConfigArchive>(a =>
            a.WidgetId == 1 && a.TriggerSource == "Schedule" && a.ScheduleId == 1)), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutSchedule_DoesNotArchiveConfig()
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
        _archiveRepoMock.Verify(r => r.CreateAsync(It.IsAny<WidgetConfigArchive>()), Times.Never);
    }

    [Fact]
    public async Task GetDataAsync_UsesStrategyForKnownSourceType()
    {
        var widget = TestDataBuilder.CreateWidget(1);
        widget.DataSource = TestDataBuilder.CreateDataSource(1, "Json Source");
        _widgetRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(widget);
        _strategyMock.Setup(s => s.CanHandle(DataSourceType.Json)).Returns(true);
        _strategyMock.Setup(s => s.LoadDataAsync(It.IsAny<Widget>(), It.IsAny<DataSource>(), It.IsAny<IHttpClientFactory>()))
            .ReturnsAsync(new { columns = new[] { "a" }, rows = new[] { new Dictionary<string, object?> { ["a"] = 1 } } });

        var result = await _service.GetDataAsync(1);

        Assert.NotNull(result);
        _strategyMock.Verify(s => s.LoadDataAsync(It.IsAny<Widget>(), It.IsAny<DataSource>(), It.IsAny<IHttpClientFactory>()), Times.Once);
    }
}