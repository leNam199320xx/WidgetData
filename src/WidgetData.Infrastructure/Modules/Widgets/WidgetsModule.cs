using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data.Json.Repositories;
using WidgetData.Infrastructure.Repositories;
using WidgetData.Infrastructure.Services;

namespace WidgetData.Infrastructure.Modules.Widgets;

public static class WidgetsModule
{
    public static IServiceCollection AddWidgetsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var businessDataProvider = configuration["Storage:BusinessDataProvider"];
        var useFileBackedBusinessData = string.Equals(businessDataProvider, "json", StringComparison.OrdinalIgnoreCase);

        if (useFileBackedBusinessData)
        {
            services.AddScoped<IWidgetRepository, FileBackedWidgetRepository>();
            services.AddScoped<IExecutionRepository, FileBackedExecutionRepository>();
            services.AddScoped<IWidgetConfigArchiveRepository, FileBackedWidgetConfigArchiveRepository>();
        }
        else
        {
            services.AddScoped<IWidgetRepository, WidgetRepository>();
            services.AddScoped<IExecutionRepository, ExecutionRepository>();
            services.AddScoped<IWidgetConfigArchiveRepository, WidgetConfigArchiveRepository>();
        }

        services.AddScoped<IJsonWidgetRepository, JsonWidgetRepository>();
        services.AddScoped<IJsonExecutionRepository, JsonExecutionRepository>();
        services.AddScoped<IJsonWidgetConfigArchiveRepository, JsonWidgetConfigArchiveRepository>();
        services.AddScoped<IJsonWidgetGroupRepository, JsonWidgetGroupRepository>();
        services.AddScoped<IJsonWidgetGroupMemberRepository, JsonWidgetGroupMemberRepository>();
        services.AddScoped<IJsonWidgetActivityRepository, JsonWidgetActivityRepository>();

        services.AddScoped<IWidgetCrudService, WidgetCrudService>();
        services.AddScoped<IWidgetExecutionService, WidgetExecutionService>();
        services.AddScoped<IWidgetService, WidgetService>();
        services.AddScoped<IWidgetConfigArchiveService, WidgetConfigArchiveService>();
        services.AddScoped<IWidgetActivityService, WidgetActivityService>();
        services.AddScoped<IWidgetGroupService, WidgetGroupService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IExportService, ExportService>();

        services.AddScoped<IDataSourceStrategy, CsvDataSourceStrategy>();
        services.AddScoped<IDataSourceStrategy, JsonDataSourceStrategy>();
        services.AddScoped<IDataSourceStrategy, ExcelDataSourceStrategy>();
        services.AddScoped<IDataSourceStrategy, RestApiDataSourceStrategy>();

        return services;
    }
}