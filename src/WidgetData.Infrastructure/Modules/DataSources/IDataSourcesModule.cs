using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace WidgetData.Infrastructure.Modules.DataSources;

public interface IDataSourcesModule
{
    static void Register(IServiceCollection services, IConfiguration configuration)
    {
    }
}