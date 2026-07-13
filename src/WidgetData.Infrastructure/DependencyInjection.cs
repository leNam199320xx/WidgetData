using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuestPDF.Infrastructure;
using WidgetData.Application.Interfaces;
using WidgetData.Domain;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data;
using WidgetData.CrossCutting;
using WidgetData.DataSources;
using WidgetData.Delivery;
using WidgetData.Identity;
using WidgetData.Pages;
using WidgetData.Widgets;
using WidgetData.Infrastructure.Startup;
using WidgetData.Infrastructure.Tools;

namespace WidgetData.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Returns true when the connection string points at a PostgreSQL server
    /// (i.e. contains "Host=" or starts with "postgresql://" / "postgres://").
    /// </summary>
    private static bool IsPostgresConnectionString(string? cs) =>
        cs != null && (
            cs.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
            cs.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
            cs.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase));

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        services.AddCommonInfrastructure(configuration);
        services.AddIdentityModule(configuration);
        services.AddWidgetsModule(configuration);
        services.AddDataSourcesModule(configuration);
        services.AddPagesModule(configuration);
        services.AddDeliveryModule(configuration);
        services.AddCrossCuttingModule(configuration);

        services.AddSingleton<IStartupInitializer, DatabaseSchemaInitializer>();
        services.AddSingleton<IStartupInitializer, DataSeedInitializer>();

        services.AddScoped<WidgetData.Infrastructure.Tools.DbToJsonMigrationTool>();
        services.AddScoped<WidgetData.Infrastructure.Data.DataSeeder>();

        return services;
    }

    private static IServiceCollection AddCommonInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

        services.AddSingleton<ILogger>(sp =>
        {
            var factory = sp.GetRequiredService<ILoggerFactory>();
            return factory.CreateLogger("Default");
        });

        var defaultConn = configuration.GetConnectionString("DefaultConnection");
        var aspireConn = configuration.GetConnectionString("widgetdata");
        var resolvedConn = aspireConn ?? defaultConn;
        var usePostgres = IsPostgresConnectionString(resolvedConn);

        services.AddDbContext<IdentityDbContext>((sp, options) =>
        {
            if (usePostgres)
                options.UseNpgsql(resolvedConn);
            else
                options.UseSqlite(resolvedConn ?? "Data Source=widgetdata.db");
        });

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            if (usePostgres)
                options.UseNpgsql(resolvedConn);
            else
                options.UseSqlite(resolvedConn ?? "Data Source=widgetdata.db");
        });

        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");
        services.AddSingleton(new WidgetData.Data.JsonDataProvider(dataDirectory));

        return services;
    }

    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        WidgetData.Identity.IdentityModule.AddIdentityModule(services, configuration);
        return services;
    }

    public static IServiceCollection AddWidgetsModule(this IServiceCollection services, IConfiguration configuration)
    {
        WidgetData.Widgets.WidgetsModule.AddWidgetsModule(services, configuration);
        return services;
    }

    public static IServiceCollection AddDataSourcesModule(this IServiceCollection services, IConfiguration configuration)
    {
        WidgetData.DataSources.DataSourcesModule.AddDataSourcesModule(services, configuration);
        return services;
    }

    public static IServiceCollection AddPagesModule(this IServiceCollection services, IConfiguration configuration)
    {
        WidgetData.Pages.PagesModule.AddPagesModule(services, configuration);
        return services;
    }

    public static IServiceCollection AddDeliveryModule(this IServiceCollection services, IConfiguration configuration)
    {
        WidgetData.Delivery.DeliveryModule.AddDeliveryModule(services, configuration);
        return services;
    }

    public static IServiceCollection AddCrossCuttingModule(this IServiceCollection services, IConfiguration configuration)
    {
        WidgetData.CrossCutting.CrossCuttingModule.AddCrossCuttingModule(services, configuration);
        return services;
    }
}