namespace WidgetData.Application.Interfaces;

public interface IPageLayoutService
{
    Task AddWidgetAsync(int pageId, int widgetId, int position, int width);
    Task RemoveWidgetAsync(int pageId, int widgetId);
    Task UpdateWidgetLayoutAsync(int pageId, int widgetId, int position, int width);
}