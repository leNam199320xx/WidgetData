using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace WidgetData.Delivery;

public interface IDeliveryModule
{
    static void Register(IServiceCollection services, IConfiguration configuration)
    {
    }
}
