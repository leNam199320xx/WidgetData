using Microsoft.EntityFrameworkCore;
using WidgetData.Application.DTOs;
using WidgetData.Domain.Entities;
using WidgetData.Infrastructure.Data;
using WidgetData.Infrastructure.Services;

namespace WidgetData.Tests.Services;

public class WidgetGroupServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly WidgetGroupService _service;

    public WidgetGroupServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new WidgetGroupService(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<WidgetGroup> SeedGroupAsync(string name, bool isActive = true, params int[] widgetIds)
    {
        var group = new WidgetGroup { Name = name, IsActive = isActive };
        _context.WidgetGroups.Add(group);
        await _context.SaveChangesAsync();

        foreach (var wId in widgetIds)
        {
            _context.WidgetGroupMembers.Add(new WidgetGroupMember { WidgetGroupId = group.Id, WidgetId = wId });
        }
        await _context.SaveChangesAsync();
        return group;
    }

    // ─── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllGroups()
    {
        await SeedGroupAsync("Group A");
        await SeedGroupAsync("Group B");

        var result = (await _service.GetAllAsync()).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmpty()
    {
        var result = await _service.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_IncludesWidgetIds()
    {
        await SeedGroupAsync("Group A", widgetIds: [1, 2, 3]);

        var result = (await _service.GetAllAsync()).ToList();

        Assert.Single(result);
        Assert.Equal(3, result[0].WidgetIds.Count);
        Assert.Contains(1, result[0].WidgetIds);
        Assert.Contains(2, result[0].WidgetIds);
        Assert.Contains(3, result[0].WidgetIds);
    }

    // ─── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var group = await SeedGroupAsync("Sales", widgetIds: [10, 20]);

        var result = await _service.GetByIdAsync(group.Id);

        Assert.NotNull(result);
        Assert.Equal("Sales", result.Name);
        Assert.Equal(2, result.WidgetIds.Count);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    // ─── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesGroupWithMembers()
    {
        var dto = new CreateWidgetGroupDto
        {
            Name = "Finance",
            Description = "Finance widgets",
            WidgetIds = [5, 6, 7]
        };

        var result = await _service.CreateAsync(dto, "admin");

        Assert.True(result.Id > 0);
        Assert.Equal("Finance", result.Name);
        Assert.Equal("Finance widgets", result.Description);
        Assert.Equal(3, result.WidgetIds.Count);
        Assert.True(result.IsActive);

        var members = await _context.WidgetGroupMembers
            .Where(m => m.WidgetGroupId == result.Id)
            .ToListAsync();
        Assert.Equal(3, members.Count);
    }

    [Fact]
    public async Task CreateAsync_DeduplicatesWidgetIds()
    {
        var dto = new CreateWidgetGroupDto
        {
            Name = "Dedup",
            WidgetIds = [1, 1, 2, 2, 3]
        };

        var result = await _service.CreateAsync(dto, "admin");

        Assert.Equal(3, result.WidgetIds.Count);
        Assert.Equal(3, await _context.WidgetGroupMembers.Where(m => m.WidgetGroupId == result.Id).CountAsync());
    }

    [Fact]
    public async Task CreateAsync_EmptyWidgetIds_CreatesGroupWithNoMembers()
    {
        var dto = new CreateWidgetGroupDto { Name = "Empty Group", WidgetIds = [] };

        var result = await _service.CreateAsync(dto, "user1");

        Assert.Empty(result.WidgetIds);
    }

    // ─── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingId_UpdatesNameAndIsActive()
    {
        var group = await SeedGroupAsync("Old Name");
        var dto = new UpdateWidgetGroupDto
        {
            Name = "New Name",
            Description = "Updated",
            IsActive = false,
            WidgetIds = []
        };

        var result = await _service.UpdateAsync(group.Id, dto);

        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        Assert.Equal("Updated", result.Description);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentId_ReturnsNull()
    {
        var dto = new UpdateWidgetGroupDto { Name = "X", WidgetIds = [] };

        var result = await _service.UpdateAsync(999, dto);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_SyncsMembership_AddsNewAndRemovesOld()
    {
        var group = await SeedGroupAsync("SyncTest", widgetIds: [1, 2, 3]);

        // Replace with [2, 4] – removes 1 and 3, adds 4
        var dto = new UpdateWidgetGroupDto
        {
            Name = "SyncTest",
            IsActive = true,
            WidgetIds = [2, 4]
        };

        var result = await _service.UpdateAsync(group.Id, dto);

        Assert.NotNull(result);
        Assert.Equal(2, result.WidgetIds.Count);
        Assert.Contains(2, result.WidgetIds);
        Assert.Contains(4, result.WidgetIds);
        Assert.DoesNotContain(1, result.WidgetIds);
        Assert.DoesNotContain(3, result.WidgetIds);
    }

    // ─── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingId_DeletesAndReturnsTrue()
    {
        var group = await SeedGroupAsync("ToDelete");

        var result = await _service.DeleteAsync(group.Id);

        Assert.True(result);
        Assert.Null(await _context.WidgetGroups.FindAsync(group.Id));
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_ReturnsFalse()
    {
        var result = await _service.DeleteAsync(999);

        Assert.False(result);
    }
}
