using Microsoft.EntityFrameworkCore;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data;

namespace WidgetData.Infrastructure.Repositories;

public class DeliveryTargetRepository : IDeliveryTargetRepository
{
    private readonly ApplicationDbContext _context;

    public DeliveryTargetRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DeliveryTarget>> GetAllAsync()
        => await _context.DeliveryTargets.ToListAsync();

    public Task<int> CountAsync()
        => _context.DeliveryTargets.CountAsync();

    public Task<int> CountActiveAsync()
        => _context.DeliveryTargets.CountAsync(t => t.IsEnabled);

    public async Task<(int Total, int Active)> GetCountsAsync()
    {
        var all = await _context.DeliveryTargets.ToListAsync();
        return (all.Count, all.Count(t => t.IsEnabled));
    }

    public Task<DeliveryTarget?> GetByIdAsync(int id)
        => _context.DeliveryTargets.FindAsync(id).AsTask();

    public async Task<DeliveryTarget> CreateAsync(DeliveryTarget target)
    {
        _context.DeliveryTargets.Add(target);
        await _context.SaveChangesAsync();
        return target;
    }

    public async Task<DeliveryTarget> UpdateAsync(DeliveryTarget target)
    {
        _context.DeliveryTargets.Update(target);
        await _context.SaveChangesAsync();
        return target;
    }

    public async Task DeleteAsync(int id)
    {
        var target = await _context.DeliveryTargets.FindAsync(id);
        if (target != null)
        {
            _context.DeliveryTargets.Remove(target);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<DeliveryTarget>> GetByWidgetAsync(int widgetId)
        => await _context.DeliveryTargets.Where(t => t.WidgetId == widgetId).ToListAsync();
}