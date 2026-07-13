using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace WidgetData.Widgets;

public interface IWidgetsModule
{
    static void Register(IServiceCollection services, IConfiguration configuration)
    {
    }
}
