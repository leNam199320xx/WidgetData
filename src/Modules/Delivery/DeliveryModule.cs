using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Interfaces;
using WidgetData.Delivery;

namespace WidgetData.Delivery;

public static class DeliveryModule
{
    public static IServiceCollection AddDeliveryModule(this IServiceCollection services, IConfiguration configuration)
    {
        var businessDataProvider = configuration["Storage:BusinessDataProvider"];
        var useFileBackedBusinessData = string.Equals(businessDataProvider, "json", StringComparison.OrdinalIgnoreCase);

        if (useFileBackedBusinessData)
        {
            services.AddScoped<IDeliveryTargetRepository, FileBackedDeliveryTargetRepository>();
            services.AddScoped<IDeliveryExecutionRepository, FileBackedDeliveryExecutionRepository>();
        }
        else
        {
            services.AddScoped<IDeliveryTargetRepository, DeliveryTargetRepository>();
            services.AddScoped<IDeliveryExecutionRepository, DeliveryExecutionRepository>();
        }

        services.AddScoped<WidgetData.Application.Interfaces.IJsonDeliveryTargetRepository, WidgetData.Data.Repositories.JsonDeliveryTargetRepository>();
        services.AddScoped<WidgetData.Application.Interfaces.IJsonDeliveryExecutionRepository, WidgetData.Data.Repositories.JsonDeliveryExecutionRepository>();

        services.AddScoped<IDeliveryTargetService, DeliveryTargetService>();
        services.AddScoped<IDeliveryExecutionService, DeliveryExecutionService>();
        services.AddScoped<WidgetData.Application.Interfaces.IDeliveryChannelStrategy, EmailDeliveryChannelStrategy>();
        services.AddScoped<WidgetData.Application.Interfaces.IDeliveryChannelStrategy, SftpDeliveryChannelStrategy>();
        services.AddScoped<WidgetData.Application.Interfaces.IDeliveryChannelStrategy, SshDeliveryChannelStrategy>();
        services.AddScoped<WidgetData.Application.Interfaces.IDeliveryChannelStrategy, HttpApiDeliveryChannelStrategy>();
        services.AddScoped<WidgetData.Application.Interfaces.IDeliveryChannelStrategy, TelegramDeliveryChannelStrategy>();
        services.AddScoped<WidgetData.Application.Interfaces.IDeliveryChannelStrategy, ZaloDeliveryChannelStrategy>();
        services.AddScoped<WidgetData.Application.Interfaces.IDeliveryChannelStrategy, FileDeliveryChannelStrategy>();
        services.AddScoped<IDeliveryDispatcher, DeliveryDispatcher>();
        services.AddScoped<IDeliveryService>(sp =>
        {
            var targetService = sp.GetRequiredService<IDeliveryTargetService>();
            var executionService = sp.GetRequiredService<IDeliveryExecutionService>();
            var dispatcher = sp.GetRequiredService<IDeliveryDispatcher>();
            return new DeliveryService(targetService, executionService, dispatcher);
        });

        return services;
    }
}
