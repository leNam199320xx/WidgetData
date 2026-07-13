using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace WidgetData.CrossCutting;

public interface ICrossCuttingModule
{
    static void Register(IServiceCollection services, IConfiguration configuration)
    {
    }
}
