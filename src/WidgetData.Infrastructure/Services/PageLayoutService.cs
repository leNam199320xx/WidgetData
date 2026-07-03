using Microsoft.Extensions.Logging;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Infrastructure.Services;

public class PageLayoutService : IPageLayoutService
{
    private readonly IPageRepository _repo;
    private readonly ILogger _logger;

    public PageLayoutService(IPageRepository repo, ILogger logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public Task AddWidgetAsync(int pageId, int widgetId, int position, int width)
        => _repo.AddWidgetAsync(pageId, widgetId, position, width);

    public Task RemoveWidgetAsync(int pageId, int widgetId)
        => _repo.RemoveWidgetAsync(pageId, widgetId);

    public Task UpdateWidgetLayoutAsync(int pageId, int widgetId, int position, int width)
        => _repo.UpdateWidgetLayoutAsync(pageId, widgetId, position, width);
}