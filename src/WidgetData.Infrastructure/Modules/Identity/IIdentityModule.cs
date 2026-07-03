using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace WidgetData.Infrastructure.Modules.Identity;

public interface IIdentityModule
{
    static void Register(IServiceCollection services, IConfiguration configuration)
    {
    }
}