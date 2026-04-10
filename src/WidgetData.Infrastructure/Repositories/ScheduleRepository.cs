using Microsoft.EntityFrameworkCore;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data;

namespace WidgetData.Infrastructure.Repositories;

public class ScheduleRepository : IScheduleRepository
{
    private readonly ApplicationDbContext _context;

    public ScheduleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<WidgetSchedule>> GetAllAsync()
        => await _context.WidgetSchedules.Include(s => s.Widget).ToListAsync();

    public async Task<IEnumerable<WidgetSchedule>> GetByWidgetIdAsync(int widgetId)
        => await _context.WidgetSchedules.Where(s => s.WidgetId == widgetId).ToListAsync();

    public async Task<WidgetSchedule?> GetByIdAsync(int id)
        => await _context.WidgetSchedules.FindAsync(id);

    public async Task<WidgetSchedule> CreateAsync(WidgetSchedule schedule)
    {
        _context.WidgetSchedules.Add(schedule);
        await _context.SaveChangesAsync();
        return schedule;
    }

    public async Task<WidgetSchedule> UpdateAsync(WidgetSchedule schedule)
    {
        _context.WidgetSchedules.Update(schedule);
        await _context.SaveChangesAsync();
        return schedule;
    }

    public async Task DeleteAsync(int id)
    {
        var s = await _context.WidgetSchedules.FindAsync(id);
        if (s != null)
        {
            _context.WidgetSchedules.Remove(s);
            await _context.SaveChangesAsync();
        }
    }
}
