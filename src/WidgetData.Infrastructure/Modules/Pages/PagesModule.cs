using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data.Json.Repositories;
using WidgetData.Infrastructure.Repositories;
using WidgetData.Infrastructure.Services;

namespace WidgetData.Infrastructure.Modules.Pages;

public static class PagesModule
{
    public static IServiceCollection AddPagesModule(this IServiceCollection services, IConfiguration configuration)
    {
        var businessDataProvider = configuration["Storage:BusinessDataProvider"];
        var useFileBackedBusinessData = string.Equals(businessDataProvider, "json", StringComparison.OrdinalIgnoreCase);

        if (useFileBackedBusinessData)
        {
            services.AddScoped<IPageRepository, FileBackedPageRepository>();
        }
        else
        {
            services.AddScoped<IPageRepository, PageRepository>();
        }

        services.AddScoped<IJsonPageRepository, JsonPageRepository>();
        services.AddScoped<IJsonPageVersionRepository, JsonPageVersionRepository>();
        services.AddScoped<IJsonPageWidgetRepository, JsonPageWidgetRepository>();

        services.AddScoped<IPageCrudService, PageCrudService>();
        services.AddScoped<IPageVersioningService, PageVersioningService>();
        services.AddScoped<IPageLayoutService, PageLayoutService>();
        services.AddScoped<IPageService, PageService>();
        services.AddScoped<IPageHtmlService, PageHtmlService>();

        return services;
    }
}