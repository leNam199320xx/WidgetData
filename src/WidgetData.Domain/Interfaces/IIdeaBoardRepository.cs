using WidgetData.Domain.Entities;

namespace WidgetData.Domain.Interfaces;

public interface IIdeaBoardRepository
{
    Task<IdeaPost> CreatePostAsync(IdeaPost post);
    Task<IdeaPost?> GetPostByIdAsync(int id);
    Task<IEnumerable<IdeaPost>> GetPostsByWidgetIdAsync(int widgetId);
    Task<IdeaPost> UpdatePostAsync(IdeaPost post);

    Task<IdeaSubscription> CreateSubscriptionAsync(IdeaSubscription subscription);
    Task<IdeaSubscription?> GetSubscriptionByIdAsync(int id);
    Task<IEnumerable<IdeaSubscription>> GetSubscriptionsByWidgetIdAsync(int widgetId);
    Task<IdeaSubscription> UpdateSubscriptionAsync(IdeaSubscription subscription);
    Task<bool> DeleteSubscriptionAsync(int id);

    Task<IdeaResult> CreateResultAsync(IdeaResult result);
    Task<IEnumerable<IdeaResult>> GetResultsByPostIdAsync(int ideaPostId);
}
