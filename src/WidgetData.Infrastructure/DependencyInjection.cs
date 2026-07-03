using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data;
using WidgetData.Infrastructure.Data.Json;
using WidgetData.Infrastructure.Data.Json.Repositories;
using WidgetData.Infrastructure.Modules.CrossCutting;
using WidgetData.Infrastructure.Modules.DataSources;
using WidgetData.Infrastructure.Modules.Delivery;
using WidgetData.Infrastructure.Modules.Identity;
using WidgetData.Infrastructure.Modules.Pages;
using WidgetData.Infrastructure.Modules.Widgets;
using WidgetData.Infrastructure.Repositories;
using WidgetData.Infrastructure.Services;
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

        return services;
    }

    private static IServiceCollection AddCommonInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

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
        services.AddSingleton(new JsonDataProvider(dataDirectory));

        return services;
    }

    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        IIdentityModule.Register(services, configuration);
        return services;
    }

    public static IServiceCollection AddWidgetsModule(this IServiceCollection services, IConfiguration configuration)
    {
        IWidgetsModule.Register(services, configuration);
        return services;
    }

    public static IServiceCollection AddDataSourcesModule(this IServiceCollection services, IConfiguration configuration)
    {
        IDataSourcesModule.Register(services, configuration);
        return services;
    }

    public static IServiceCollection AddPagesModule(this IServiceCollection services, IConfiguration configuration)
    {
        IPagesModule.Register(services, configuration);
        return services;
    }

    public static IServiceCollection AddDeliveryModule(this IServiceCollection services, IConfiguration configuration)
    {
        IDeliveryModule.Register(services, configuration);
        return services;
    }

    public static IServiceCollection AddCrossCuttingModule(this IServiceCollection services, IConfiguration configuration)
    {
        ICrossCuttingModule.Register(services, configuration);
        return services;
    }
}