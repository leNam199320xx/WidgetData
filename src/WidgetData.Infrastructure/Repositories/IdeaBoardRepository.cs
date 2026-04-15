using Microsoft.EntityFrameworkCore;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data;

namespace WidgetData.Infrastructure.Repositories;

public class IdeaBoardRepository : IIdeaBoardRepository
{
    private readonly ApplicationDbContext _context;

    public IdeaBoardRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IdeaPost> CreatePostAsync(IdeaPost post)
    {
        _context.IdeaPosts.Add(post);
        await _context.SaveChangesAsync();
        return post;
    }

    public async Task<IdeaPost?> GetPostByIdAsync(int id)
        => await _context.IdeaPosts
            .Include(p => p.Results).ThenInclude(r => r.IdeaSubscription)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IEnumerable<IdeaPost>> GetPostsByWidgetIdAsync(int widgetId)
        => await _context.IdeaPosts
            .Where(p => p.WidgetId == widgetId)
            .Include(p => p.Results).ThenInclude(r => r.IdeaSubscription)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<IdeaPost> UpdatePostAsync(IdeaPost post)
    {
        _context.IdeaPosts.Update(post);
        await _context.SaveChangesAsync();
        return post;
    }

    public async Task<IdeaSubscription> CreateSubscriptionAsync(IdeaSubscription subscription)
    {
        _context.IdeaSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();
        return subscription;
    }

    public async Task<IdeaSubscription?> GetSubscriptionByIdAsync(int id)
        => await _context.IdeaSubscriptions.FindAsync(id);

    public async Task<IEnumerable<IdeaSubscription>> GetSubscriptionsByWidgetIdAsync(int widgetId)
        => await _context.IdeaSubscriptions
            .Where(s => s.WidgetId == widgetId)
            .OrderBy(s => s.Name)
            .ToListAsync();

    public async Task<IdeaSubscription> UpdateSubscriptionAsync(IdeaSubscription subscription)
    {
        _context.IdeaSubscriptions.Update(subscription);
        await _context.SaveChangesAsync();
        return subscription;
    }

    public async Task<bool> DeleteSubscriptionAsync(int id)
    {
        var sub = await _context.IdeaSubscriptions.FindAsync(id);
        if (sub == null) return false;
        _context.IdeaSubscriptions.Remove(sub);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IdeaResult> CreateResultAsync(IdeaResult result)
    {
        _context.IdeaResults.Add(result);
        await _context.SaveChangesAsync();
        return result;
    }

    public async Task<IEnumerable<IdeaResult>> GetResultsByPostIdAsync(int ideaPostId)
        => await _context.IdeaResults
            .Where(r => r.IdeaPostId == ideaPostId)
            .Include(r => r.IdeaSubscription)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
}
