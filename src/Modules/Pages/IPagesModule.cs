using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace WidgetData.Pages;

public interface IPagesModule
{
    static void Register(IServiceCollection services, IConfiguration configuration)
    {
    }
}
