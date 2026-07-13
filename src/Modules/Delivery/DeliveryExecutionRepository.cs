using Microsoft.EntityFrameworkCore;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;
using WidgetData.Domain;

namespace WidgetData.Delivery;

public class DeliveryExecutionRepository : IDeliveryExecutionRepository
{
    private readonly ApplicationDbContext _context;

    public DeliveryExecutionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DeliveryExecution>> GetByTargetAsync(int deliveryTargetId)
        => await _context.DeliveryExecutions.Where(e => e.DeliveryTargetId == deliveryTargetId).OrderByDescending(e => e.ExecutedAt).ToListAsync();

    public async Task<IEnumerable<DeliveryExecution>> GetByWidgetAsync(int widgetId)
    {
        var targetIds = await _context.DeliveryTargets.Where(t => t.WidgetId == widgetId).Select(t => t.Id).ToListAsync();
        return await _context.DeliveryExecutions.Where(e => targetIds.Contains(e.DeliveryTargetId)).OrderByDescending(e => e.ExecutedAt).ToListAsync();
    }

    public async Task<DeliveryExecution> CreateAsync(DeliveryExecution execution)
    {
        _context.DeliveryExecutions.Add(execution);
        await _context.SaveChangesAsync();
        return execution;
    }

    public async Task<DeliveryExecution> UpdateAsync(DeliveryExecution execution)
    {
        _context.DeliveryExecutions.Update(execution);
        await _context.SaveChangesAsync();
        return execution;
    }
}
