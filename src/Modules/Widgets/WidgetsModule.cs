using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Interfaces;
using WidgetData.Widgets;

namespace WidgetData.Widgets;

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

        services.AddScoped<WidgetData.Application.Interfaces.IJsonWidgetRepository, WidgetData.Data.Repositories.JsonWidgetRepository>();
        services.AddScoped<WidgetData.Application.Interfaces.IJsonExecutionRepository, WidgetData.Data.Repositories.JsonExecutionRepository>();
        services.AddScoped<WidgetData.Application.Interfaces.IJsonWidgetConfigArchiveRepository, WidgetData.Data.Repositories.JsonWidgetConfigArchiveRepository>();
        services.AddScoped<WidgetData.Application.Interfaces.IJsonWidgetGroupRepository, WidgetData.Data.Repositories.JsonWidgetGroupRepository>();
        services.AddScoped<WidgetData.Application.Interfaces.IJsonWidgetGroupMemberRepository, WidgetData.Data.Repositories.JsonWidgetGroupMemberRepository>();
        services.AddScoped<WidgetData.Application.Interfaces.IJsonWidgetActivityRepository, WidgetData.Data.Repositories.JsonWidgetActivityRepository>();

        services.AddScoped<IWidgetCrudService, WidgetCrudService>();
        services.AddScoped<IWidgetExecutionService, WidgetExecutionService>();
        services.AddScoped<IWidgetService>(sp =>
        {
            var widgetRepo = sp.GetRequiredService<IWidgetRepository>();
            var executionRepo = sp.GetRequiredService<IExecutionRepository>();
            var groupMemberRepo = sp.GetRequiredService<IJsonWidgetGroupMemberRepository>();
            var archiveRepo = sp.GetRequiredService<IWidgetConfigArchiveRepository>();
            var scheduleRepo = sp.GetRequiredService<IScheduleRepository>();
            var auditService = sp.GetRequiredService<IAuditService>();
            var logger = sp.GetRequiredService<ILogger<WidgetService>>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var strategies = sp.GetRequiredService<IEnumerable<IDataSourceStrategy>>();
            var tenantContext = sp.GetRequiredService<ITenantContext>();
            return new WidgetService(widgetRepo, executionRepo, groupMemberRepo, archiveRepo, scheduleRepo, auditService, logger, httpClientFactory, strategies, tenantContext);
        });
        services.AddScoped<IWidgetConfigArchiveService, WidgetConfigArchiveService>();
        services.AddScoped<IWidgetGroupService, WidgetGroupService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IExportService, ExportService>();

        return services;
    }
}
