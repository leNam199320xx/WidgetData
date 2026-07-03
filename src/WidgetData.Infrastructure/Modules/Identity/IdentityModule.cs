using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Repositories;
using WidgetData.Infrastructure.Services;

namespace WidgetData.Infrastructure.Modules.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITenantRepository, TenantRepository>();

        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<ITenantService, TenantService>();

        return services;
    }
}