using Microsoft.EntityFrameworkCore;
using WidgetData.Domain;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;

namespace WidgetData.DataSources;

public class DataSourceRepository : IDataSourceRepository
{
    private readonly ApplicationDbContext _context;

    public DataSourceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DataSource>> GetAllAsync()
        => await _context.DataSources.ToListAsync();

    public Task<int> CountAsync()
        => _context.DataSources.CountAsync();

    public Task<int> CountActiveAsync()
        => _context.DataSources.CountAsync(d => d.IsActive);

    public async Task<(int Total, int Active)> GetCountsAsync()
    {
        var total = await _context.DataSources.CountAsync();
        var active = await _context.DataSources.CountAsync(d => d.IsActive);
        return (total, active);
    }

    public async Task<DataSource?> GetByIdAsync(int id)
        => await _context.DataSources.FindAsync(id);

    public async Task<DataSource> CreateAsync(DataSource dataSource)
    {
        _context.DataSources.Add(dataSource);
        await _context.SaveChangesAsync();
        return dataSource;
    }

    public async Task<DataSource> UpdateAsync(DataSource dataSource)
    {
        _context.DataSources.Update(dataSource);
        await _context.SaveChangesAsync();
        return dataSource;
    }

    public async Task DeleteAsync(int id)
    {
        var ds = await _context.DataSources.FindAsync(id);
        if (ds != null)
        {
            _context.DataSources.Remove(ds);
            await _context.SaveChangesAsync();
        }
    }
}
