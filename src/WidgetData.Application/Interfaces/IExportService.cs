namespace WidgetData.Application.Interfaces;

public interface IExportService
{
    Task<byte[]> ExportAsync(int widgetId, string format);
    string GetContentType(string format);
    string GetFileName(int widgetId, string format);
}
