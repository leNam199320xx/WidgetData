using Microsoft.EntityFrameworkCore;
using WidgetData.Application.Interfaces;
using WidgetData.Domain;
using WidgetData.Domain.Entities;

namespace WidgetData.Tests.Services;

public class FakeJsonWidgetGroupRepository : IJsonWidgetGroupRepository
{
    private readonly ApplicationDbContext _context;
    public FakeJsonWidgetGroupRepository(ApplicationDbContext context) => _context = context;

    public Task<WidgetGroup?> GetByIdAsync(int id) => Task.FromResult(
        _context.WidgetGroups.Include(g => g.Members).FirstOrDefault(g => g.Id == id));

    public Task<List<WidgetGroup>> GetAllAsync() => Task.FromResult(
        _context.WidgetGroups.Include(g => g.Members).ToList());

    public Task<List<WidgetGroup>> GetByTenantAsync(int? tenantId) => Task.FromResult(
        _context.WidgetGroups.Include(g => g.Members).ToList());

    public async Task<WidgetGroup> CreateAsync(WidgetGroup entity)
    {
        _context.WidgetGroups.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<WidgetGroup> UpdateAsync(WidgetGroup entity)
    {
        _context.Entry(entity).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var group = await _context.WidgetGroups.FindAsync(id);
        if (group == null) return false;
        _context.WidgetGroups.Remove(group);
        await _context.SaveChangesAsync();
        return true;
    }

    public Task<bool> ExistsAsync(int id) => Task.FromResult(_context.WidgetGroups.Any(g => g.Id == id));

    public Task<List<WidgetGroup>> GetActiveGroupsAsync() => Task.FromResult(
        _context.WidgetGroups.Where(g => g.IsActive).Include(g => g.Members).ToList());
}

public class FakeJsonWidgetGroupMemberRepository : IJsonWidgetGroupMemberRepository
{
    private readonly ApplicationDbContext _context;
    public FakeJsonWidgetGroupMemberRepository(ApplicationDbContext context) => _context = context;

    public Task<WidgetGroupMember?> GetByIdAsync(int id)
    {
        var groupId = id / 1_000_000;
        var widgetId = id % 1_000_000;
        return Task.FromResult(_context.WidgetGroupMembers.FirstOrDefault(m => m.WidgetGroupId == groupId && m.WidgetId == widgetId));
    }

    public Task<List<WidgetGroupMember>> GetAllAsync() => Task.FromResult(_context.WidgetGroupMembers.ToList());

    public Task<List<WidgetGroupMember>> GetByTenantAsync(int? tenantId) => Task.FromResult(_context.WidgetGroupMembers.ToList());

    public async Task<WidgetGroupMember> CreateAsync(WidgetGroupMember entity)
    {
        _context.WidgetGroupMembers.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<WidgetGroupMember> UpdateAsync(WidgetGroupMember entity)
    {
        _context.Entry(entity).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var groupId = id / 1_000_000;
        var widgetId = id % 1_000_000;
        var member = _context.WidgetGroupMembers.FirstOrDefault(m => m.WidgetGroupId == groupId && m.WidgetId == widgetId);
        if (member == null) return false;
        _context.WidgetGroupMembers.Remove(member);
        await _context.SaveChangesAsync();
        return true;
    }

    public Task<bool> ExistsAsync(int id)
    {
        var groupId = id / 1_000_000;
        var widgetId = id % 1_000_000;
        return Task.FromResult(_context.WidgetGroupMembers.Any(m => m.WidgetGroupId == groupId && m.WidgetId == widgetId));
    }

    public Task<List<WidgetGroupMember>> GetByGroupAsync(int groupId) => Task.FromResult(
        _context.WidgetGroupMembers.Where(m => m.WidgetGroupId == groupId).ToList());

    public Task<List<WidgetGroupMember>> GetByWidgetAsync(int widgetId) => Task.FromResult(
        _context.WidgetGroupMembers.Where(m => m.WidgetId == widgetId).ToList());
}
