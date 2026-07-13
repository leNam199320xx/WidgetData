using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Interfaces;
using WidgetData.Pages;

namespace WidgetData.Pages;

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

        services.AddScoped<WidgetData.Application.Interfaces.IJsonPageRepository, WidgetData.Data.Repositories.JsonPageRepository>();
        services.AddScoped<WidgetData.Application.Interfaces.IJsonPageVersionRepository, WidgetData.Data.Repositories.JsonPageVersionRepository>();
        services.AddScoped<WidgetData.Application.Interfaces.IJsonPageWidgetRepository, WidgetData.Data.Repositories.JsonPageWidgetRepository>();

        services.AddScoped<IPageCrudService, PageCrudService>();
        services.AddScoped<IPageVersioningService, PageVersioningService>();
        services.AddScoped<IPageLayoutService, PageLayoutService>();
        services.AddScoped<IPageService>(sp =>
        {
            var pageRepo = sp.GetRequiredService<IPageRepository>();
            return new PageService(pageRepo);
        });
        services.AddScoped<IPageHtmlService, PageHtmlService>();

        return services;
    }
}
