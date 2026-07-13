using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace WidgetData.DataSources;

public interface IDataSourcesModule
{
    static void Register(IServiceCollection services, IConfiguration configuration)
    {
    }
}
