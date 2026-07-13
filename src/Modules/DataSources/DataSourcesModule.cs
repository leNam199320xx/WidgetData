using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Interfaces;
using WidgetData.DataSources;

namespace WidgetData.DataSources;

public static class DataSourcesModule
{
    public static IServiceCollection AddDataSourcesModule(this IServiceCollection services, IConfiguration configuration)
    {
        var businessDataProvider = configuration["Storage:BusinessDataProvider"];
        var useFileBackedBusinessData = string.Equals(businessDataProvider, "json", StringComparison.OrdinalIgnoreCase);

        if (useFileBackedBusinessData)
        {
            services.AddScoped<IDataSourceRepository, FileBackedDataSourceRepository>();
        }
        else
        {
            services.AddScoped<IDataSourceRepository, DataSourceRepository>();
        }

        services.AddScoped<WidgetData.Application.Interfaces.IJsonDataSourceRepository, WidgetData.Data.Repositories.JsonDataSourceRepository>();

        services.AddScoped<IDataSourceCrudService, DataSourceCrudService>();
        services.AddScoped<IDataSourceUploadService, DataSourceUploadService>();
        services.AddScoped<IDataSourceConnectivityTestService, DataSourceConnectivityTestService>();
        services.AddScoped<IDataSourceService>(sp =>
        {
            var repo = sp.GetRequiredService<IDataSourceRepository>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var hostEnvironment = sp.GetRequiredService<Microsoft.Extensions.Hosting.IHostEnvironment>();
            var tenantContext = sp.GetRequiredService<WidgetData.Domain.Interfaces.ITenantContext>();
            return new DataSourceService(repo, httpClientFactory, hostEnvironment, tenantContext);
        });

        services.AddScoped<IDataSourceValidator, CsvDataSourceValidator>();
        services.AddScoped<IDataSourceValidator, JsonDataSourceValidator>();
        services.AddScoped<IDataSourceValidator, ExcelDataSourceValidator>();
        services.AddScoped<IDataSourceValidator, RestApiDataSourceValidator>();

        services.AddScoped<IFileHandler, FileHandler>();

        return services;
    }
}
