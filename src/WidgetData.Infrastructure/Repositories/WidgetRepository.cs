using Microsoft.EntityFrameworkCore;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data;

namespace WidgetData.Infrastructure.Repositories;

public class WidgetRepository : IWidgetRepository
{
    private readonly ApplicationDbContext _context;

    public WidgetRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Widget>> GetAllAsync()
        => await _context.Widgets.Include(w => w.DataSource).ToListAsync();

    public async Task<Widget?> GetByIdAsync(int id)
        => await _context.Widgets.Include(w => w.DataSource).FirstOrDefaultAsync(w => w.Id == id);

    public async Task<Widget> CreateAsync(Widget widget)
    {
        _context.Widgets.Add(widget);
        await _context.SaveChangesAsync();
        return widget;
    }

    public async Task<Widget> UpdateAsync(Widget widget)
    {
        _context.Widgets.Update(widget);
        await _context.SaveChangesAsync();
        return widget;
    }

    public async Task DeleteAsync(int id)
    {
        var widget = await _context.Widgets.FindAsync(id);
        if (widget != null)
        {
            _context.Widgets.Remove(widget);
            await _context.SaveChangesAsync();
        }
    }
}
