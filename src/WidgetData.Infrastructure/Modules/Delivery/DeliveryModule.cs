using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data.Json.Repositories;
using WidgetData.Infrastructure.Repositories;
using WidgetData.Infrastructure.Services;

namespace WidgetData.Infrastructure.Modules.Delivery;

public static class DeliveryModule
{
    public static IServiceCollection AddDeliveryModule(this IServiceCollection services, IConfiguration configuration)
    {
        var businessDataProvider = configuration["Storage:BusinessDataProvider"];
        var useFileBackedBusinessData = string.Equals(businessDataProvider, "json", StringComparison.OrdinalIgnoreCase);

        if (useFileBackedBusinessData)
        {
            services.AddScoped<IWidgetRepository, FileBackedWidgetRepository>();
            services.AddScoped<IExecutionRepository, FileBackedExecutionRepository>();
            services.AddScoped<IDeliveryTargetRepository, FileBackedDeliveryTargetRepository>();
            services.AddScoped<IDeliveryExecutionRepository, FileBackedDeliveryExecutionRepository>();
        }
        else
        {
            services.AddScoped<IWidgetRepository, WidgetRepository>();
            services.AddScoped<IExecutionRepository, ExecutionRepository>();
            services.AddScoped<IDeliveryTargetRepository, DeliveryTargetRepository>();
            services.AddScoped<IDeliveryExecutionRepository, DeliveryExecutionRepository>();
        }

        services.AddScoped<IJsonDeliveryTargetRepository, JsonDeliveryTargetRepository>();
        services.AddScoped<IJsonDeliveryExecutionRepository, JsonDeliveryExecutionRepository>();

        services.AddScoped<IDeliveryTargetService, DeliveryTargetService>();
        services.AddScoped<IDeliveryExecutionService, DeliveryExecutionService>();
        services.AddScoped<IDeliveryChannelStrategy, EmailDeliveryChannelStrategy>();
        services.AddScoped<IDeliveryChannelStrategy, SftpDeliveryChannelStrategy>();
        services.AddScoped<IDeliveryChannelStrategy, SshDeliveryChannelStrategy>();
        services.AddScoped<IDeliveryChannelStrategy, HttpApiDeliveryChannelStrategy>();
        services.AddScoped<IDeliveryChannelStrategy, TelegramDeliveryChannelStrategy>();
        services.AddScoped<IDeliveryChannelStrategy, ZaloDeliveryChannelStrategy>();
        services.AddScoped<IDeliveryChannelStrategy, FileDeliveryChannelStrategy>();
        services.AddScoped<IDeliveryDispatcher, DeliveryDispatcher>();
        services.AddScoped<IDeliveryService, DeliveryService>();

        return services;
    }
}