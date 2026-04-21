using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data;
using WidgetData.Infrastructure.Repositories;
using WidgetData.Infrastructure.Services;

namespace WidgetData.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection") ?? "Data Source=widgetdata.db"));

        services.AddScoped<IWidgetRepository, WidgetRepository>();
        services.AddScoped<IDataSourceRepository, DataSourceRepository>();
        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        services.AddScoped<IExecutionRepository, ExecutionRepository>();
        services.AddScoped<IWidgetConfigArchiveRepository, WidgetConfigArchiveRepository>();
        services.AddScoped<IIdeaBoardRepository, IdeaBoardRepository>();
        services.AddScoped<IWidgetActivityRepository, WidgetActivityRepository>();

        services.AddScoped<IWidgetService, WidgetService>();
        services.AddScoped<IDataSourceService, DataSourceService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IWidgetGroupService, WidgetGroupService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<IDeliveryService, DeliveryService>();
        services.AddScoped<IWidgetConfigArchiveService, WidgetConfigArchiveService>();
        services.AddScoped<IIdeaBoardService, IdeaBoardService>();
        services.AddScoped<IPageHtmlService, PageHtmlService>();
        services.AddScoped<IWidgetActivityService, WidgetActivityService>();

        services.AddHostedService<InactivityMonitorService>();

        services.AddHttpClient();
        services.AddScoped<DataSeeder>();

        return services;
    }
}
