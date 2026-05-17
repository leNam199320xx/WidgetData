using Microsoft.EntityFrameworkCore;

namespace WidgetData.Infrastructure.Tools;

/// <summary>
/// Extension methods for EntityFrameworkCore
/// </summary>
public static class EfCoreExtensions
{
    /// <summary>
    /// Convert IQueryable to List asynchronously (shorthand)
    /// </summary>
    public static async Task<List<T>> ToListAsync<T>(this IQueryable<T> query)
    {
        return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(query);
    }
}
