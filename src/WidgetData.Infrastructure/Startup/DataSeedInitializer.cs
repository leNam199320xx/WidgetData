using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WidgetData.Application.Interfaces;
using WidgetData.Domain;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Infrastructure.Data;

namespace WidgetData.Infrastructure.Startup;

public class DataSeedInitializer : IStartupInitializer
{
    public string Name => "Data Seed";
    public int Order => 2;

    public async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataSeeder>>();

        var seeder = new DataSeeder(context, userManager, roleManager, environment, logger);
        await seeder.SeedAsync();
    }
}