namespace WidgetData.Application.Interfaces;

public interface IDataSourceConnectivityTestService
{
    Task<string> TestConnectionAsync(int id);
}