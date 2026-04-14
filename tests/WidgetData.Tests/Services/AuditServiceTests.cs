using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WidgetData.Infrastructure.Data;
using WidgetData.Infrastructure.Services;

namespace WidgetData.Tests.Services;

public class AuditServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _service;

    public AuditServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new AuditService(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    // ─── LogAsync – basic persistence ────────────────────────────────────────

    [Fact]
    public async Task LogAsync_WithRequiredFields_StoresAuditLog()
    {
        await _service.LogAsync("CREATE");

        var log = await _context.AuditLogs.SingleAsync();

        Assert.Equal("CREATE", log.Action);
    }

    [Fact]
    public async Task LogAsync_WithAllFields_StoresAllFields()
    {
        await _service.LogAsync(
            action: "UPDATE",
            entityType: "Widget",
            entityId: "42",
            oldValues: null,
            newValues: null,
            userId: "user1",
            userEmail: "user@example.com",
            ipAddress: "127.0.0.1",
            userAgent: "Mozilla/5.0",
            notes: "test note");

        var log = await _context.AuditLogs.SingleAsync();

        Assert.Equal("UPDATE", log.Action);
        Assert.Equal("Widget", log.EntityType);
        Assert.Equal("42", log.EntityId);
        Assert.Equal("user1", log.UserId);
        Assert.Equal("user@example.com", log.UserEmail);
        Assert.Equal("127.0.0.1", log.IpAddress);
        Assert.Equal("Mozilla/5.0", log.UserAgent);
        Assert.Equal("test note", log.Notes);
    }

    [Fact]
    public async Task LogAsync_WithNullOptionalFields_StoresNulls()
    {
        await _service.LogAsync("DELETE");

        var log = await _context.AuditLogs.SingleAsync();

        Assert.Null(log.EntityType);
        Assert.Null(log.EntityId);
        Assert.Null(log.OldValues);
        Assert.Null(log.NewValues);
        Assert.Null(log.UserId);
        Assert.Null(log.UserEmail);
        Assert.Null(log.IpAddress);
        Assert.Null(log.UserAgent);
        Assert.Null(log.Notes);
    }

    [Fact]
    public async Task LogAsync_WithOldAndNewValues_SerializesToJson()
    {
        var oldVal = new { Name = "OldName" };
        var newVal = new { Name = "NewName" };

        await _service.LogAsync("UPDATE", oldValues: oldVal, newValues: newVal);

        var log = await _context.AuditLogs.SingleAsync();

        Assert.NotNull(log.OldValues);
        Assert.NotNull(log.NewValues);
        using var oldDoc = JsonDocument.Parse(log.OldValues);
        using var newDoc = JsonDocument.Parse(log.NewValues);
        Assert.Equal("OldName", oldDoc.RootElement.GetProperty("Name").GetString());
        Assert.Equal("NewName", newDoc.RootElement.GetProperty("Name").GetString());
    }

    [Fact]
    public async Task LogAsync_SetsTimestampNearUtcNow()
    {
        var before = DateTime.UtcNow;
        await _service.LogAsync("VIEW");
        var after = DateTime.UtcNow;

        var log = await _context.AuditLogs.SingleAsync();

        Assert.InRange(log.Timestamp, before.AddSeconds(-1), after.AddSeconds(1));
    }

    [Fact]
    public async Task LogAsync_MultipleCalls_StoresMultipleLogs()
    {
        await _service.LogAsync("CREATE", entityType: "Widget");
        await _service.LogAsync("UPDATE", entityType: "Widget");
        await _service.LogAsync("DELETE", entityType: "DataSource");

        Assert.Equal(3, await _context.AuditLogs.CountAsync());
    }
}
