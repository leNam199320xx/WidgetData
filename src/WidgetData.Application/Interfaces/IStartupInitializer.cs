namespace WidgetData.Application.Interfaces;

public interface IStartupInitializer
{
    string Name { get; }
    int Order { get; }
    Task InitializeAsync(IServiceProvider serviceProvider);
}