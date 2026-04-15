using WidgetData.Application.DTOs;

namespace WidgetData.Application.Interfaces;

public interface IIdeaBoardService
{
    Task<IdeaPostDto> CreatePostAsync(CreateIdeaPostDto dto, string userId);
    Task<IdeaPostDto?> GetPostByIdAsync(int id);
    Task<IEnumerable<IdeaPostDto>> GetPostsByWidgetIdAsync(int widgetId);

    Task<IdeaSubscriptionDto> CreateSubscriptionAsync(CreateIdeaSubscriptionDto dto, string userId);
    Task<IdeaSubscriptionDto?> UpdateSubscriptionAsync(int id, UpdateIdeaSubscriptionDto dto);
    Task<IEnumerable<IdeaSubscriptionDto>> GetSubscriptionsByWidgetIdAsync(int widgetId);
    Task<bool> DeleteSubscriptionAsync(int id);

    Task<IdeaResultDto?> SubmitResultAsync(int ideaPostId, int subscriptionId, CreateIdeaResultDto dto);
    Task<IEnumerable<IdeaResultDto>> GetResultsByPostIdAsync(int ideaPostId);
}
