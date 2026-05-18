using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data;
using WidgetData.Infrastructure.Data.Json;
using WidgetData.Infrastructure.Data.Json.Repositories;
using WidgetData.Infrastructure.Repositories;
using WidgetData.Infrastructure.Services;
using WidgetData.Infrastructure.Tools;

namespace WidgetData.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        // ITenantContext is scoped (per-request) – must be registered before DbContext
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

        // Register IdentityDbContext (User Management)
        services.AddDbContext<IdentityDbContext>((sp, options) =>
        {
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection") ?? "Data Source=widgetdata.db");
        });

        // Keep ApplicationDbContext for backward compatibility (will be removed later)
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection") ?? "Data Source=widgetdata.db");
        });

        // Register JSON Data Provider
        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");
        services.AddSingleton(new JsonDataProvider(dataDirectory));

        // Register JSON Repositories (Business Data)
        services.AddScoped<IJsonWidgetRepository, JsonWidgetRepository>();
        services.AddScoped<IJsonDataSourceRepository, JsonDataSourceRepository>();
        services.AddScoped<IJsonScheduleRepository, JsonScheduleRepository>();
        services.AddScoped<IJsonExecutionRepository, JsonExecutionRepository>();
        services.AddScoped<IJsonPageRepository, JsonPageRepository>();
        services.AddScoped<IJsonPageVersionRepository, JsonPageVersionRepository>();
        services.AddScoped<IJsonPageWidgetRepository, JsonPageWidgetRepository>();
        services.AddScoped<IJsonWidgetGroupRepository, JsonWidgetGroupRepository>();
        services.AddScoped<IJsonWidgetGroupMemberRepository, JsonWidgetGroupMemberRepository>();
        services.AddScoped<IJsonWidgetConfigArchiveRepository, JsonWidgetConfigArchiveRepository>();
        services.AddScoped<IJsonDeliveryTargetRepository, JsonDeliveryTargetRepository>();
        services.AddScoped<IJsonDeliveryExecutionRepository, JsonDeliveryExecutionRepository>();
        services.AddScoped<IJsonIdeaPostRepository, JsonIdeaPostRepository>();
        services.AddScoped<IJsonIdeaSubscriptionRepository, JsonIdeaSubscriptionRepository>();
        services.AddScoped<IJsonIdeaResultRepository, JsonIdeaResultRepository>();
        services.AddScoped<IJsonFormSubmissionRepository, JsonFormSubmissionRepository>();
        services.AddScoped<IJsonWidgetActivityRepository, JsonWidgetActivityRepository>();

        var businessDataProvider = configuration["Storage:BusinessDataProvider"];
        var useFileBackedBusinessData = string.Equals(businessDataProvider, "json", StringComparison.OrdinalIgnoreCase);

        if (useFileBackedBusinessData)
        {
            services.AddScoped<IWidgetRepository, FileBackedWidgetRepository>();
            services.AddScoped<IDataSourceRepository, FileBackedDataSourceRepository>();
            services.AddScoped<IScheduleRepository, FileBackedScheduleRepository>();
            services.AddScoped<IExecutionRepository, FileBackedExecutionRepository>();
            services.AddScoped<IWidgetConfigArchiveRepository, FileBackedWidgetConfigArchiveRepository>();
            services.AddScoped<IPageRepository, FileBackedPageRepository>();
            // JSON repos are already registered above and will be used by WidgetGroupService
        }
        else
        {
            // Keep DB Repositories for now (backward compatibility)
            services.AddScoped<IWidgetRepository, WidgetRepository>();
            services.AddScoped<IDataSourceRepository, DataSourceRepository>();
            services.AddScoped<IScheduleRepository, ScheduleRepository>();
            services.AddScoped<IExecutionRepository, ExecutionRepository>();
            services.AddScoped<IWidgetConfigArchiveRepository, WidgetConfigArchiveRepository>();
            services.AddScoped<IPageRepository, PageRepository>();
            // Override JSON group repos with EF-backed adapters so WidgetGroupService works in EF mode
            services.AddScoped<IJsonWidgetGroupRepository, EfWidgetGroupRepositoryAdapter>();
            services.AddScoped<IJsonWidgetGroupMemberRepository, EfWidgetGroupMemberRepositoryAdapter>();
        }

        services.AddScoped<IIdeaBoardRepository, IdeaBoardRepository>();
        services.AddScoped<IWidgetActivityRepository, WidgetActivityRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();

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
        services.AddScoped<IFormService, FormService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IPageService, PageService>();

        services.AddHostedService<InactivityMonitorService>();

        // Register migration tool
        services.AddScoped<DbToJsonMigrationTool>();

        services.AddHttpClient();
        services.AddScoped<DataSeeder>();

        return services;
    }
}
