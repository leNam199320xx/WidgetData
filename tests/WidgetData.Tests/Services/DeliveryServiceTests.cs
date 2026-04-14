using Microsoft.EntityFrameworkCore;
using Moq;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Infrastructure.Data;
using WidgetData.Infrastructure.Services;

namespace WidgetData.Tests.Services;

public class DeliveryServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IExportService> _exportServiceMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly DeliveryService _service;

    public DeliveryServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _exportServiceMock = new Mock<IExportService>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _service = new DeliveryService(_context, _exportServiceMock.Object, _httpClientFactoryMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<DeliveryTarget> SeedTargetAsync(int widgetId = 1, DeliveryType type = DeliveryType.Email,
        bool isEnabled = true, string? configuration = null)
    {
        var target = new DeliveryTarget
        {
            WidgetId = widgetId,
            Name = $"Target-{widgetId}",
            Type = type,
            Configuration = configuration,
            IsEnabled = isEnabled
        };
        _context.DeliveryTargets.Add(target);
        await _context.SaveChangesAsync();
        return target;
    }

    // ─── GetTargetsAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetTargetsAsync_ReturnsTargetsForWidget()
    {
        await SeedTargetAsync(widgetId: 1);
        await SeedTargetAsync(widgetId: 1);
        await SeedTargetAsync(widgetId: 2);

        var result = (await _service.GetTargetsAsync(1)).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.Equal(1, t.WidgetId));
    }

    [Fact]
    public async Task GetTargetsAsync_NoTargets_ReturnsEmpty()
    {
        var result = await _service.GetTargetsAsync(99);

        Assert.Empty(result);
    }

    // ─── GetTargetByIdAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetTargetByIdAsync_ExistingId_ReturnsDto()
    {
        var target = await SeedTargetAsync(widgetId: 1, type: DeliveryType.Csv);

        var result = await _service.GetTargetByIdAsync(target.Id);

        Assert.NotNull(result);
        Assert.Equal(target.Id, result.Id);
        Assert.Equal(1, result.WidgetId);
        Assert.Equal(DeliveryType.Csv, result.Type);
    }

    [Fact]
    public async Task GetTargetByIdAsync_NonExistentId_ReturnsNull()
    {
        var result = await _service.GetTargetByIdAsync(999);

        Assert.Null(result);
    }

    // ─── CreateTargetAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateTargetAsync_ValidDto_CreatesAndReturnsDto()
    {
        var dto = new CreateDeliveryTargetDto
        {
            WidgetId = 1,
            Name = "Email Report",
            Type = DeliveryType.Email,
            Configuration = "{\"to\":\"admin@example.com\"}",
            IsEnabled = true
        };

        var result = await _service.CreateTargetAsync(dto, "user1");

        Assert.True(result.Id > 0);
        Assert.Equal("Email Report", result.Name);
        Assert.Equal(1, result.WidgetId);
        Assert.Equal(DeliveryType.Email, result.Type);
        Assert.Equal("user1", result.CreatedBy);
        Assert.Equal(1, await _context.DeliveryTargets.CountAsync());
    }

    // ─── UpdateTargetAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateTargetAsync_ExistingId_UpdatesAndReturnsDto()
    {
        var target = await SeedTargetAsync(widgetId: 1, type: DeliveryType.Email);
        var dto = new UpdateDeliveryTargetDto
        {
            WidgetId = 1,
            Name = "Updated Name",
            Type = DeliveryType.Csv,
            Configuration = "{}",
            IsEnabled = false
        };

        var result = await _service.UpdateTargetAsync(target.Id, dto);

        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal(DeliveryType.Csv, result.Type);
        Assert.False(result.IsEnabled);
    }

    [Fact]
    public async Task UpdateTargetAsync_NonExistentId_ReturnsNull()
    {
        var dto = new UpdateDeliveryTargetDto { WidgetId = 1, Name = "X", Type = DeliveryType.Email };

        var result = await _service.UpdateTargetAsync(999, dto);

        Assert.Null(result);
    }

    // ─── DeleteTargetAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteTargetAsync_ExistingId_DeletesAndReturnsTrue()
    {
        var target = await SeedTargetAsync();

        var result = await _service.DeleteTargetAsync(target.Id);

        Assert.True(result);
        Assert.Equal(0, await _context.DeliveryTargets.CountAsync());
    }

    [Fact]
    public async Task DeleteTargetAsync_NonExistentId_ReturnsFalse()
    {
        var result = await _service.DeleteTargetAsync(999);

        Assert.False(result);
    }

    // ─── DeliverAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DeliverAsync_NonExistentTarget_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.DeliverAsync(1, 999, "user1"));
    }

    [Fact]
    public async Task DeliverAsync_WithCsvTarget_CreatesExecutionRecord()
    {
        var target = await SeedTargetAsync(widgetId: 1, type: DeliveryType.Csv);
        _exportServiceMock.Setup(s => s.ExportAsync(1, "csv")).ReturnsAsync(new byte[] { 1, 2, 3 });
        _exportServiceMock.Setup(s => s.GetFileName(1, "csv")).Returns("widget_1_export.csv");

        var result = await _service.DeliverAsync(1, target.Id, "user1");

        Assert.Equal(target.Id, result.DeliveryTargetId);
        Assert.Equal("user1", result.TriggeredBy);
        // Execution record is persisted
        Assert.Equal(1, await _context.DeliveryExecutions.CountAsync());
    }

    [Fact]
    public async Task DeliverAsync_WithCsvTarget_WhenExportFails_ExecutionStatusIsFailed()
    {
        var target = await SeedTargetAsync(widgetId: 1, type: DeliveryType.Csv);
        _exportServiceMock.Setup(s => s.ExportAsync(1, "csv"))
            .ThrowsAsync(new InvalidOperationException("export error"));
        _exportServiceMock.Setup(s => s.GetFileName(1, "csv")).Returns("widget_1_export.csv");

        var result = await _service.DeliverAsync(1, target.Id, "user1");

        Assert.Equal(ExecutionStatus.Failed, result.Status);
        Assert.Contains("export error", result.Message);
    }

    // ─── GetExecutionsAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetExecutionsAsync_ReturnsExecutionsOrderedByDateDescending()
    {
        var target = await SeedTargetAsync(widgetId: 1);
        _context.DeliveryExecutions.Add(new DeliveryExecution
        {
            DeliveryTargetId = target.Id,
            Status = ExecutionStatus.Success,
            ExecutedAt = DateTime.UtcNow.AddHours(-2)
        });
        _context.DeliveryExecutions.Add(new DeliveryExecution
        {
            DeliveryTargetId = target.Id,
            Status = ExecutionStatus.Failed,
            ExecutedAt = DateTime.UtcNow.AddHours(-1)
        });
        await _context.SaveChangesAsync();

        var result = (await _service.GetExecutionsAsync(1)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(ExecutionStatus.Failed, result[0].Status);
        Assert.Equal(ExecutionStatus.Success, result[1].Status);
    }

    [Fact]
    public async Task GetExecutionsAsync_NoTargets_ReturnsEmpty()
    {
        var result = await _service.GetExecutionsAsync(99);

        Assert.Empty(result);
    }
}
