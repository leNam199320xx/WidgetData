using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data.Json.Repositories;
using WidgetData.Infrastructure.Repositories;
using WidgetData.Infrastructure.Services;

namespace WidgetData.Infrastructure.Modules.DataSources;

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

        services.AddScoped<IJsonDataSourceRepository, JsonDataSourceRepository>();

        services.AddScoped<IDataSourceCrudService, DataSourceCrudService>();
        services.AddScoped<IDataSourceUploadService, DataSourceUploadService>();
        services.AddScoped<IDataSourceConnectivityTestService, DataSourceConnectivityTestService>();
        services.AddScoped<IDataSourceService, DataSourceService>();

        services.AddScoped<IDataSourceValidator, CsvDataSourceValidator>();
        services.AddScoped<IDataSourceValidator, JsonDataSourceValidator>();
        services.AddScoped<IDataSourceValidator, ExcelDataSourceValidator>();
        services.AddScoped<IDataSourceValidator, RestApiDataSourceValidator>();

        services.AddScoped<IFileHandler, FileHandler>();

        return services;
    }
}