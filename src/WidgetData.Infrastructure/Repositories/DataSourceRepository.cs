using Microsoft.EntityFrameworkCore;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data;

namespace WidgetData.Infrastructure.Repositories;

public class DataSourceRepository : IDataSourceRepository
{
    private readonly ApplicationDbContext _context;

    public DataSourceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DataSource>> GetAllAsync()
        => await _context.DataSources.ToListAsync();

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
