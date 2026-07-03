using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data;
using WidgetData.Infrastructure.Data.Json.Repositories;
using WidgetData.Infrastructure.Repositories;
using WidgetData.Infrastructure.Services;
using WidgetData.Infrastructure.Tools;

namespace WidgetData.Infrastructure.Modules.CrossCutting;

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
            services.AddScoped<IScheduleRepository, ScheduleRepository>();
        }

        services.AddScoped<IJsonScheduleRepository, JsonScheduleRepository>();
        services.AddScoped<IIdeaBoardRepository, IdeaBoardRepository>();
        services.AddScoped<IWidgetActivityRepository, WidgetActivityRepository>();

        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPageHtmlService, PageHtmlService>();
        services.AddScoped<IFormService, FormService>();
        services.AddScoped<IIdeaBoardService, IdeaBoardService>();
        services.AddScoped<IWidgetActivityService, WidgetActivityService>();

        services.AddHostedService<InactivityMonitorService>();

        services.AddScoped<DbToJsonMigrationTool>();

        services.AddHttpClient();
        services.AddScoped<DataSeeder>();

        return services;
    }
}