using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace WidgetData.Identity;

public interface IIdentityModule
{
    static void Register(IServiceCollection services, IConfiguration configuration)
    {
    }
}
