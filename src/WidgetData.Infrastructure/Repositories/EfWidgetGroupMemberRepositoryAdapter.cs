using Microsoft.EntityFrameworkCore;
using WidgetData.Domain.Entities;
using WidgetData.Infrastructure.Data;
using WidgetData.Infrastructure.Data.Json.Repositories;

namespace WidgetData.Infrastructure.Repositories;

public sealed class EfWidgetGroupMemberRepositoryAdapter : IJsonWidgetGroupMemberRepository
{
    private readonly ApplicationDbContext _context;

    public EfWidgetGroupMemberRepositoryAdapter(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WidgetGroupMember?> GetByIdAsync(int id)
    {
        var (groupId, widgetId) = FromCompositeId(id);
        return await _context.WidgetGroupMembers.FirstOrDefaultAsync(m => m.WidgetGroupId == groupId && m.WidgetId == widgetId);
    }

    public async Task<List<WidgetGroupMember>> GetAllAsync()
        => await _context.WidgetGroupMembers.AsNoTracking().ToListAsync();

    public async Task<List<WidgetGroupMember>> GetByTenantAsync(int? tenantId)
        => await _context.WidgetGroupMembers.AsNoTracking().ToListAsync();

    public async Task<WidgetGroupMember> CreateAsync(WidgetGroupMember entity)
    {
        var exists = await _context.WidgetGroupMembers
            .AnyAsync(m => m.WidgetGroupId == entity.WidgetGroupId && m.WidgetId == entity.WidgetId);
        if (!exists)
        {
            _context.WidgetGroupMembers.Add(entity);
            await _context.SaveChangesAsync();
        }
        return entity;
    }

    public async Task<WidgetGroupMember> UpdateAsync(WidgetGroupMember entity)
    {
        var exists = await _context.WidgetGroupMembers
            .AnyAsync(m => m.WidgetGroupId == entity.WidgetGroupId && m.WidgetId == entity.WidgetId);
        if (!exists)
        {
            _context.WidgetGroupMembers.Add(entity);
            await _context.SaveChangesAsync();
        }

        return entity;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var (groupId, widgetId) = FromCompositeId(id);
        var existing = await _context.WidgetGroupMembers
            .FirstOrDefaultAsync(m => m.WidgetGroupId == groupId && m.WidgetId == widgetId);
        if (existing == null) return false;
        _context.WidgetGroupMembers.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
        => await GetByIdAsync(id) != null;

    public async Task<List<WidgetGroupMember>> GetByGroupAsync(int groupId)
        => await _context.WidgetGroupMembers.AsNoTracking().Where(m => m.WidgetGroupId == groupId).ToListAsync();

    public async Task<List<WidgetGroupMember>> GetByWidgetAsync(int widgetId)
        => await _context.WidgetGroupMembers.AsNoTracking().Where(m => m.WidgetId == widgetId).ToListAsync();

    private static (int groupId, int widgetId) FromCompositeId(int id)
        => (id / 1_000_000, id % 1_000_000);
}
