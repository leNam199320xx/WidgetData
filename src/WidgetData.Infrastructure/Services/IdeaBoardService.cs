using System.Text;
using System.Text.Json;
using WidgetData.Application.DTOs;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Infrastructure.Services;

public class IdeaBoardService : IIdeaBoardService
{
    private readonly IIdeaBoardRepository _repo;
    private readonly IHttpClientFactory _httpClientFactory;

    public IdeaBoardService(IIdeaBoardRepository repo, IHttpClientFactory httpClientFactory)
    {
        _repo = repo;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IdeaPostDto> CreatePostAsync(CreateIdeaPostDto dto, string userId)
    {
        var post = new IdeaPost
        {
            WidgetId = dto.WidgetId,
            Title = dto.Title,
            Content = dto.Content,
            Labels = dto.Labels,
            Status = "Pending",
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };
        post = await _repo.CreatePostAsync(post);

        // Dispatch to matching subscriptions
        var subscriptions = (await _repo.GetSubscriptionsByWidgetIdAsync(dto.WidgetId))
            .Where(s => s.IsActive)
            .ToList();

        var postLabels = ParseLabels(dto.Labels);
        var matchingSubscriptions = subscriptions
            .Where(s => string.IsNullOrWhiteSpace(s.LabelFilter) || ParseLabels(s.LabelFilter).Intersect(postLabels, StringComparer.OrdinalIgnoreCase).Any())
            .ToList();

        foreach (var sub in matchingSubscriptions)
        {
            await DispatchAsync(post, sub);
        }

        if (matchingSubscriptions.Any())
        {
            post.Status = "Dispatched";
            post.ProcessedAt = DateTime.UtcNow;
            await _repo.UpdatePostAsync(post);
        }

        return await MapPostToDtoAsync(post);
    }

    private async Task DispatchAsync(IdeaPost post, IdeaSubscription sub)
    {
        if (string.IsNullOrWhiteSpace(sub.WebhookUrl))
        {
            // Internal: store an auto-result
            await _repo.CreateResultAsync(new IdeaResult
            {
                IdeaPostId = post.Id,
                IdeaSubscriptionId = sub.Id,
                ResultContent = $"Đã nhận: {post.Title}",
                Status = "Received",
                CreatedAt = DateTime.UtcNow
            });
            return;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var payload = new
            {
                postId = post.Id,
                widgetId = post.WidgetId,
                title = post.Title,
                content = post.Content,
                labels = post.Labels,
                subscriptionId = sub.Id
            };
            var json = JsonSerializer.Serialize(payload);
            var response = await client.PostAsync(sub.WebhookUrl,
                new StringContent(json, Encoding.UTF8, "application/json"));

            var resultContent = await response.Content.ReadAsStringAsync();
            await _repo.CreateResultAsync(new IdeaResult
            {
                IdeaPostId = post.Id,
                IdeaSubscriptionId = sub.Id,
                ResultContent = resultContent,
                Status = response.IsSuccessStatusCode ? "Delivered" : "Error",
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            await _repo.CreateResultAsync(new IdeaResult
            {
                IdeaPostId = post.Id,
                IdeaSubscriptionId = sub.Id,
                ResultContent = $"Lỗi gửi webhook: {ex.Message}",
                Status = "Error",
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    public async Task<IdeaPostDto?> GetPostByIdAsync(int id)
    {
        var post = await _repo.GetPostByIdAsync(id);
        return post == null ? null : await MapPostToDtoAsync(post);
    }

    public async Task<IEnumerable<IdeaPostDto>> GetPostsByWidgetIdAsync(int widgetId)
    {
        var posts = await _repo.GetPostsByWidgetIdAsync(widgetId);
        return posts.Select(MapPostToDto);
    }

    public async Task<IdeaSubscriptionDto> CreateSubscriptionAsync(CreateIdeaSubscriptionDto dto, string userId)
    {
        var sub = new IdeaSubscription
        {
            WidgetId = dto.WidgetId,
            Name = dto.Name,
            LabelFilter = dto.LabelFilter,
            WebhookUrl = dto.WebhookUrl,
            IsActive = dto.IsActive,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };
        sub = await _repo.CreateSubscriptionAsync(sub);
        return MapSubscriptionToDto(sub);
    }

    public async Task<IdeaSubscriptionDto?> UpdateSubscriptionAsync(int id, UpdateIdeaSubscriptionDto dto)
    {
        var sub = await _repo.GetSubscriptionByIdAsync(id);
        if (sub == null) return null;
        sub.Name = dto.Name;
        sub.LabelFilter = dto.LabelFilter;
        sub.WebhookUrl = dto.WebhookUrl;
        sub.IsActive = dto.IsActive;
        sub = await _repo.UpdateSubscriptionAsync(sub);
        return MapSubscriptionToDto(sub);
    }

    public async Task<IEnumerable<IdeaSubscriptionDto>> GetSubscriptionsByWidgetIdAsync(int widgetId)
    {
        var subs = await _repo.GetSubscriptionsByWidgetIdAsync(widgetId);
        return subs.Select(MapSubscriptionToDto);
    }

    public Task<bool> DeleteSubscriptionAsync(int id)
        => _repo.DeleteSubscriptionAsync(id);

    public async Task<IdeaResultDto?> SubmitResultAsync(int ideaPostId, int subscriptionId, CreateIdeaResultDto dto)
    {
        var post = await _repo.GetPostByIdAsync(ideaPostId);
        if (post == null) return null;

        var result = new IdeaResult
        {
            IdeaPostId = ideaPostId,
            IdeaSubscriptionId = subscriptionId,
            ResultContent = dto.ResultContent,
            Status = dto.Status,
            CreatedAt = DateTime.UtcNow
        };
        result = await _repo.CreateResultAsync(result);

        var sub = await _repo.GetSubscriptionByIdAsync(subscriptionId);
        return MapResultToDto(result, sub?.Name);
    }

    public async Task<IEnumerable<IdeaResultDto>> GetResultsByPostIdAsync(int ideaPostId)
    {
        var results = await _repo.GetResultsByPostIdAsync(ideaPostId);
        return results.Select(r => MapResultToDto(r, r.IdeaSubscription?.Name));
    }

    private static ISet<string> ParseLabels(string? labels)
        => (labels ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static Task<IdeaPostDto> MapPostToDtoAsync(IdeaPost p)
        => Task.FromResult(MapPostToDto(p));

    private static IdeaPostDto MapPostToDto(IdeaPost p) => new()
    {
        Id = p.Id,
        WidgetId = p.WidgetId,
        Title = p.Title,
        Content = p.Content,
        Labels = p.Labels,
        Status = p.Status,
        CreatedBy = p.CreatedBy,
        CreatedAt = p.CreatedAt,
        ProcessedAt = p.ProcessedAt,
        Results = p.Results.Select(r => MapResultToDto(r, r.IdeaSubscription?.Name)).ToList()
    };

    private static IdeaSubscriptionDto MapSubscriptionToDto(IdeaSubscription s) => new()
    {
        Id = s.Id,
        WidgetId = s.WidgetId,
        Name = s.Name,
        LabelFilter = s.LabelFilter,
        WebhookUrl = s.WebhookUrl,
        IsActive = s.IsActive,
        CreatedBy = s.CreatedBy,
        CreatedAt = s.CreatedAt
    };

    private static IdeaResultDto MapResultToDto(IdeaResult r, string? subName) => new()
    {
        Id = r.Id,
        IdeaPostId = r.IdeaPostId,
        IdeaSubscriptionId = r.IdeaSubscriptionId,
        SubscriptionName = subName,
        ResultContent = r.ResultContent,
        Status = r.Status,
        CreatedAt = r.CreatedAt
    };
}
