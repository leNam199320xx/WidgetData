using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Infrastructure.Data;
using WidgetData.Infrastructure.Data.Json;
using WidgetData.Infrastructure.Data.Json.Repositories;

namespace WidgetData.Infrastructure.Startup;

public class DatabaseSchemaInitializer : IStartupInitializer
{
    public string Name => "Database Schema";
    public int Order => 1;

public async Task InitializeAsync(IServiceProvider serviceProvider)
{
    var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
    using var scope = scopeFactory.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("DatabaseSchemaInitializer");
    var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

    await EnsureDatabaseSchemaReadyAsync(context, environment, logger);
}

    private static async Task EnsureDatabaseSchemaReadyAsync(ApplicationDbContext context, IHostEnvironment environment, ILogger logger)
    {
        if (context.Database.IsNpgsql() || context.Database.IsSqlite())
        {
            if (context.Database.IsNpgsql())
            {
                await context.Database.MigrateAsync();
                return;
            }
        }

        var hasCoreSchema = await HasCoreSchemaAsync(context);
        if (!hasCoreSchema)
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException("Database schema is incomplete. Automatic database reset is allowed only in Development.");

            logger.LogWarning("Detected incomplete database schema. Recreating development database.");
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
            return;
        }

        var hasCompatibleData = await HasCompatibleCoreDataAsync(context);
        if (!hasCompatibleData)
        {
            if (!environment.IsDevelopment() && !environment.IsEnvironment("Test"))
                throw new InvalidOperationException("Database data format is incompatible. Automatic database reset is allowed only in Development/Test.");

            logger.LogWarning("Detected incompatible database data format. Recreating database for current model.");
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
        }
    }

    private static async Task<bool> HasCoreSchemaAsync(ApplicationDbContext context)
    {
        return await HasTableAsync(context, "Widgets")
               && await HasTableAsync(context, "DataSources")
               && await HasTableAsync(context, "AspNetUsers");
    }

    private static async Task<bool> HasTableAsync(ApplicationDbContext context, string tableName)
    {
        var connection = context.Database.GetDbConnection();
        var openedHere = connection.State != System.Data.ConnectionState.Open;
        if (openedHere)
            await connection.OpenAsync();

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name";
            var p = cmd.CreateParameter();
            p.ParameterName = "@name";
            p.Value = tableName;
            cmd.Parameters.Add(p);
            var scalar = await cmd.ExecuteScalarAsync();
            var count = scalar as long? ?? 0;
            return count > 0;
        }
        finally
        {
            if (openedHere)
                await connection.CloseAsync();
        }
    }

    private static async Task<bool> HasCompatibleCoreDataAsync(ApplicationDbContext context)
    {
        try
        {
            await context.Widgets
                .AsNoTracking()
                .Select(w => new { w.Id, w.LastExecutedAt, w.LastActivityAt })
                .Take(1)
                .ToListAsync();

            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}