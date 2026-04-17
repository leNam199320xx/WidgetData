using Moq;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Services;
using WidgetData.Tests.TestData;

namespace WidgetData.Tests.Services;

public class ScheduleServiceTests
{
    private readonly Mock<IScheduleRepository> _repoMock;
    private readonly Mock<WidgetData.Application.Interfaces.IWidgetService> _widgetServiceMock;
    private readonly ScheduleService _service;

    public ScheduleServiceTests()
    {
        _repoMock = new Mock<IScheduleRepository>();
        _widgetServiceMock = new Mock<WidgetData.Application.Interfaces.IWidgetService>();
        _service = new ScheduleService(_repoMock.Object, _widgetServiceMock.Object);
    }

    // ─── GetAllAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var schedules = new List<WidgetSchedule>
        {
            TestDataBuilder.CreateSchedule(1, 1),
            TestDataBuilder.CreateSchedule(2, 2)
        };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(schedules);

        var result = (await _service.GetAllAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("0 * * * *", result[0].CronExpression);
    }

    [Fact]
    public async Task GetAllAsync_EmptyList_ReturnsEmpty()
    {
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<WidgetSchedule>());

        var result = await _service.GetAllAsync();

        Assert.Empty(result);
    }

    // ─── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedSchedule()
    {
        var dto = TestDataBuilder.CreateScheduleDto(1);
        var created = new WidgetSchedule
        {
            Id = 10,
            WidgetId = dto.WidgetId,
            CronExpression = dto.CronExpression,
            Timezone = dto.Timezone,
            IsEnabled = dto.IsEnabled,
            RetryOnFailure = dto.RetryOnFailure,
            MaxRetries = dto.MaxRetries,
            CreatedAt = DateTime.UtcNow,
            Widget = TestDataBuilder.CreateWidget(dto.WidgetId)
        };
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<WidgetSchedule>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(dto);

        Assert.Equal(10, result.Id);
        Assert.Equal(dto.WidgetId, result.WidgetId);
        Assert.Equal(dto.CronExpression, result.CronExpression);
        Assert.Equal(dto.Timezone, result.Timezone);
        Assert.Equal(dto.IsEnabled, result.IsEnabled);
        _repoMock.Verify(r => r.CreateAsync(It.Is<WidgetSchedule>(s =>
            s.WidgetId == dto.WidgetId &&
            s.CronExpression == dto.CronExpression)), Times.Once);
    }

    // ─── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingId_ReturnsUpdatedDto()
    {
        var existing = TestDataBuilder.CreateSchedule(1, 1, true);
        var dto = TestDataBuilder.UpdateScheduleDto(1);
        var updated = new WidgetSchedule
        {
            Id = 1,
            WidgetId = 1,
            CronExpression = dto.CronExpression,
            Timezone = dto.Timezone,
            IsEnabled = dto.IsEnabled,
            RetryOnFailure = dto.RetryOnFailure,
            MaxRetries = dto.MaxRetries,
            UpdatedAt = DateTime.UtcNow,
            Widget = TestDataBuilder.CreateWidget(1)
        };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<WidgetSchedule>())).ReturnsAsync(updated);

        var result = await _service.UpdateAsync(1, dto);

        Assert.NotNull(result);
        Assert.Equal("0 0 * * *", result.CronExpression);
        Assert.False(result.IsEnabled);
        Assert.Equal(5, result.MaxRetries);
        _repoMock.Verify(r => r.UpdateAsync(It.Is<WidgetSchedule>(s =>
            s.UpdatedAt != null)), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((WidgetSchedule?)null);

        var result = await _service.UpdateAsync(99, TestDataBuilder.UpdateScheduleDto());

        Assert.Null(result);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<WidgetSchedule>()), Times.Never);
    }

    // ─── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsTrue()
    {
        var schedule = TestDataBuilder.CreateSchedule(1);
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(schedule);
        _repoMock.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(1);

        Assert.True(result);
        _repoMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_ReturnsFalse()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((WidgetSchedule?)null);

        var result = await _service.DeleteAsync(99);

        Assert.False(result);
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    // ─── EnableAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task EnableAsync_DisabledSchedule_EnablesAndReturnsTrue()
    {
        var schedule = TestDataBuilder.CreateSchedule(1, 1, isEnabled: false);
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(schedule);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<WidgetSchedule>())).ReturnsAsync(schedule);

        var result = await _service.EnableAsync(1);

        Assert.True(result);
        _repoMock.Verify(r => r.UpdateAsync(It.Is<WidgetSchedule>(s =>
            s.IsEnabled == true && s.UpdatedAt != null)), Times.Once);
    }

    [Fact]
    public async Task EnableAsync_NonExistentId_ReturnsFalse()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((WidgetSchedule?)null);

        var result = await _service.EnableAsync(99);

        Assert.False(result);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<WidgetSchedule>()), Times.Never);
    }

    // ─── DisableAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DisableAsync_EnabledSchedule_DisablesAndReturnsTrue()
    {
        var schedule = TestDataBuilder.CreateSchedule(1, 1, isEnabled: true);
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(schedule);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<WidgetSchedule>())).ReturnsAsync(schedule);

        var result = await _service.DisableAsync(1);

        Assert.True(result);
        _repoMock.Verify(r => r.UpdateAsync(It.Is<WidgetSchedule>(s =>
            s.IsEnabled == false && s.UpdatedAt != null)), Times.Once);
    }

    [Fact]
    public async Task DisableAsync_NonExistentId_ReturnsFalse()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((WidgetSchedule?)null);

        var result = await _service.DisableAsync(99);

        Assert.False(result);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<WidgetSchedule>()), Times.Never);
    }

    // ─── Mapping ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_MapsWidgetNameFromNavigationProperty()
    {
        var dto = TestDataBuilder.CreateScheduleDto(1);
        var created = TestDataBuilder.CreateSchedule(10, 1);
        created.Widget.Name = "Sales Chart";
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<WidgetSchedule>())).ReturnsAsync(created);

        var result = await _service.CreateAsync(dto);

        Assert.Equal("Sales Chart", result.WidgetName);
    }
}
