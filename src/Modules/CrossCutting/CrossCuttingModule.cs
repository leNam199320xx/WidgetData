using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Interfaces;
using WidgetData.CrossCutting;

namespace WidgetData.CrossCutting;

public static class CrossCuttingModule
{
    public static IServiceCollection AddCrossCuttingModule(this IServiceCollection services, IConfiguration configuration)
    {
        var businessDataProvider = configuration["Storage:BusinessDataProvider"];
        var useFileBackedBusinessData = string.Equals(businessDataProvider, "json", StringComparison.OrdinalIgnoreCase);

        if (useFileBackedBusinessData)
        {
            services.AddScoped<IScheduleRepository, FileBackedScheduleRepository>();
        }
        else
        {
            services.AddScoped<IScheduleRepository, Repositories.ScheduleRepository>();
        }

        services.AddScoped<WidgetData.Application.Interfaces.IJsonScheduleRepository, WidgetData.Data.Repositories.JsonScheduleRepository>();
        services.AddScoped<IIdeaBoardRepository, Repositories.IdeaBoardRepository>();
        services.AddScoped<IWidgetActivityRepository, Repositories.WidgetActivityRepository>();

        services.AddScoped<IScheduleService, Services.ScheduleService>();
        services.AddScoped<IAuditService, Services.AuditService>();
        services.AddScoped<IFormService, Services.FormService>();
        services.AddScoped<IIdeaBoardService, Services.IdeaBoardService>();
        services.AddScoped<IWidgetActivityService, Services.WidgetActivityService>();

        services.AddHostedService<Services.InactivityMonitorService>();

        return services;
    }
}
