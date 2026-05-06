using Microsoft.EntityFrameworkCore;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data;

namespace WidgetData.Infrastructure.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly ApplicationDbContext _context;

    public TenantRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Tenant>> GetAllAsync()
        => await _context.Tenants.OrderBy(t => t.Name).ToListAsync();

    public async Task<Tenant?> GetByIdAsync(int id)
        => await _context.Tenants.FirstOrDefaultAsync(t => t.Id == id);

    public async Task<Tenant?> GetBySlugAsync(string slug)
        => await _context.Tenants.FirstOrDefaultAsync(t => t.Slug == slug);

    public async Task<Tenant> CreateAsync(Tenant tenant)
    {
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();
        return tenant;
    }

    public async Task<Tenant> UpdateAsync(Tenant tenant)
    {
        tenant.UpdatedAt = DateTime.UtcNow;
        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync();
        return tenant;
    }

    public async Task DeleteAsync(int id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant != null)
        {
            _context.Tenants.Remove(tenant);
            await _context.SaveChangesAsync();
        }
    }
}
