using Microsoft.EntityFrameworkCore;
using WidgetData.Domain.Entities;
using WidgetData.Infrastructure.Data;
using WidgetData.Infrastructure.Data.Json.Repositories;

namespace WidgetData.Infrastructure.Repositories;

/// <summary>
/// Adapts ApplicationDbContext (EF) to IJsonWidgetGroupRepository so that WidgetGroupService
/// can work in both EF mode and JSON mode without code changes.
/// </summary>
public sealed class EfWidgetGroupRepositoryAdapter : IJsonWidgetGroupRepository
{
    private readonly ApplicationDbContext _context;

    public EfWidgetGroupRepositoryAdapter(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WidgetGroup?> GetByIdAsync(int id)
        => await _context.WidgetGroups.Include(g => g.Members).AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id);

    public async Task<List<WidgetGroup>> GetAllAsync()
        => await _context.WidgetGroups.Include(g => g.Members).AsNoTracking().ToListAsync();

    public async Task<List<WidgetGroup>> GetByTenantAsync(int? tenantId)
    {
        if (!tenantId.HasValue)
            return await GetAllAsync();

        return await _context.WidgetGroups.Include(g => g.Members).AsNoTracking()
            .Where(g => g.TenantId == tenantId || g.TenantId == null)
            .ToListAsync();
    }

    public async Task<WidgetGroup> CreateAsync(WidgetGroup entity)
    {
        _context.WidgetGroups.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<WidgetGroup> UpdateAsync(WidgetGroup entity)
    {
        var tracked = await _context.WidgetGroups.FindAsync(entity.Id);
        if (tracked != null)
        {
            _context.Entry(tracked).CurrentValues.SetValues(entity);
        }
        else
        {
            _context.WidgetGroups.Update(entity);
        }
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

    public async Task<bool> ExistsAsync(int id)
        => await _context.WidgetGroups.AnyAsync(g => g.Id == id);

    public async Task<List<WidgetGroup>> GetActiveGroupsAsync()
        => await _context.WidgetGroups.Include(g => g.Members).AsNoTracking()
            .Where(g => g.IsActive).ToListAsync();
}
