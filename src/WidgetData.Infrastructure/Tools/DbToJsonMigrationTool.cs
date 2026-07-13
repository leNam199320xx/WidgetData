using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WidgetData.Domain;
using WidgetData.Domain.Entities;
using WidgetData.Data;
using WidgetData.Data.Repositories;

namespace WidgetData.Infrastructure.Tools;

/// <summary>
/// Tool để export dữ liệu từ ApplicationDbContext sang JSON files
/// Sử dụng cho migration từ Database sang JSON-based storage
/// </summary>
public class DbToJsonMigrationTool
{
    private readonly ApplicationDbContext _dbContext;
    private readonly JsonDataProvider _jsonProvider;
    private readonly ILogger<DbToJsonMigrationTool> _logger;

    public DbToJsonMigrationTool(
        ApplicationDbContext dbContext,
        JsonDataProvider jsonProvider,
        ILogger<DbToJsonMigrationTool> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _jsonProvider = jsonProvider ?? throw new ArgumentNullException(nameof(jsonProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Migrate tất cả dữ liệu từ Database sang JSON files
    /// </summary>
    public async Task MigrateAllAsync()
    {
        _logger.LogInformation("Starting database to JSON migration...");

        try
        {
            await MigrateWidgetsAsync();
            await MigrateDataSourcesAsync();
            await MigrateWidgetSchedulesAsync();
            await MigrateWidgetExecutionsAsync();
            await MigratePagesAsync();
            await MigratePageVersionsAsync();
            await MigratePageWidgetsAsync();
            await MigrateWidgetGroupsAsync();
            await MigrateWidgetGroupMembersAsync();
            await MigrateWidgetConfigArchivesAsync();
            await MigrateDeliveryTargetsAsync();
            await MigrateDeliveryExecutionsAsync();
            await MigrateIdeaPostsAsync();
            await MigrateIdeaSubscriptionsAsync();
            await MigrateIdeaResultsAsync();
            await MigrateFormSubmissionsAsync();
            await MigrateWidgetApiActivitiesAsync();

            _logger.LogInformation("Database to JSON migration completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database to JSON migration failed!");
            throw;
        }
    }

    private async Task MigrateWidgetsAsync()
    {
        _logger.LogInformation("Migrating Widgets...");
        var widgets = await _dbContext.Widgets.ToListAsync();
        var repo = new JsonWidgetRepository(_jsonProvider);

        foreach (var widget in widgets)
        {
            await repo.CreateAsync(widget);
        }

        _logger.LogInformation($"Migrated {widgets.Count} widgets");
    }

    private async Task MigrateDataSourcesAsync()
    {
        _logger.LogInformation("Migrating DataSources...");
        var dataSources = await _dbContext.DataSources.ToListAsync();
        var repo = new JsonDataSourceRepository(_jsonProvider);

        foreach (var ds in dataSources)
        {
            await repo.CreateAsync(ds);
        }

        _logger.LogInformation($"Migrated {dataSources.Count} data sources");
    }

    private async Task MigrateWidgetSchedulesAsync()
    {
        _logger.LogInformation("Migrating WidgetSchedules...");
        var schedules = await _dbContext.WidgetSchedules.ToListAsync();
        var repo = new JsonScheduleRepository(_jsonProvider);

        foreach (var schedule in schedules)
        {
            await repo.CreateAsync(schedule);
        }

        _logger.LogInformation($"Migrated {schedules.Count} widget schedules");
    }

    private async Task MigrateWidgetExecutionsAsync()
    {
        _logger.LogInformation("Migrating WidgetExecutions...");
        var executions = await _dbContext.WidgetExecutions.ToListAsync();
        var repo = new JsonExecutionRepository(_jsonProvider);

        foreach (var execution in executions)
        {
            await repo.CreateAsync(execution);
        }

        _logger.LogInformation($"Migrated {executions.Count} widget executions");
    }

    private async Task MigratePagesAsync()
    {
        _logger.LogInformation("Migrating Pages...");
        var pages = await _dbContext.Pages.ToListAsync();
        var repo = new JsonPageRepository(_jsonProvider);

        foreach (var page in pages)
        {
            await repo.CreateAsync(page);
        }

        _logger.LogInformation($"Migrated {pages.Count} pages");
    }

    private async Task MigratePageVersionsAsync()
    {
        _logger.LogInformation("Migrating PageVersions...");
        var versions = await _dbContext.PageVersions.ToListAsync();
        var repo = new JsonPageVersionRepository(_jsonProvider);

        foreach (var version in versions)
        {
            await repo.CreateAsync(version);
        }

        _logger.LogInformation($"Migrated {versions.Count} page versions");
    }

    private async Task MigratePageWidgetsAsync()
    {
        _logger.LogInformation("Migrating PageWidgets...");
        var pageWidgets = await _dbContext.PageWidgets.ToListAsync();
        var repo = new JsonPageWidgetRepository(_jsonProvider);

        foreach (var pw in pageWidgets)
        {
            await repo.CreateAsync(pw);
        }

        _logger.LogInformation($"Migrated {pageWidgets.Count} page widgets");
    }

    private async Task MigrateWidgetGroupsAsync()
    {
        _logger.LogInformation("Migrating WidgetGroups...");
        var groups = await _dbContext.WidgetGroups.ToListAsync();
        var repo = new JsonWidgetGroupRepository(_jsonProvider);

        foreach (var group in groups)
        {
            await repo.CreateAsync(group);
        }

        _logger.LogInformation($"Migrated {groups.Count} widget groups");
    }

    private async Task MigrateWidgetGroupMembersAsync()
    {
        _logger.LogInformation("Migrating WidgetGroupMembers...");
        var members = await _dbContext.WidgetGroupMembers.ToListAsync();
        var repo = new JsonWidgetGroupMemberRepository(_jsonProvider);

        foreach (var member in members)
        {
            await repo.CreateAsync(member);
        }

        _logger.LogInformation($"Migrated {members.Count} widget group members");
    }

    private async Task MigrateWidgetConfigArchivesAsync()
    {
        _logger.LogInformation("Migrating WidgetConfigArchives...");
        var archives = await _dbContext.WidgetConfigArchives.ToListAsync();
        var repo = new JsonWidgetConfigArchiveRepository(_jsonProvider);

        foreach (var archive in archives)
        {
            await repo.CreateAsync(archive);
        }

        _logger.LogInformation($"Migrated {archives.Count} widget config archives");
    }

    private async Task MigrateDeliveryTargetsAsync()
    {
        _logger.LogInformation("Migrating DeliveryTargets...");
        var targets = await _dbContext.DeliveryTargets.ToListAsync();
        var repo = new JsonDeliveryTargetRepository(_jsonProvider);

        foreach (var target in targets)
        {
            await repo.CreateAsync(target);
        }

        _logger.LogInformation($"Migrated {targets.Count} delivery targets");
    }

    private async Task MigrateDeliveryExecutionsAsync()
    {
        _logger.LogInformation("Migrating DeliveryExecutions...");
        var executions = await _dbContext.DeliveryExecutions.ToListAsync();
        var repo = new JsonDeliveryExecutionRepository(_jsonProvider);

        foreach (var execution in executions)
        {
            await repo.CreateAsync(execution);
        }

        _logger.LogInformation($"Migrated {executions.Count} delivery executions");
    }

    private async Task MigrateIdeaPostsAsync()
    {
        _logger.LogInformation("Migrating IdeaPosts...");
        var posts = await _dbContext.IdeaPosts.ToListAsync();
        var repo = new JsonIdeaPostRepository(_jsonProvider);

        foreach (var post in posts)
        {
            await repo.CreateAsync(post);
        }

        _logger.LogInformation($"Migrated {posts.Count} idea posts");
    }

    private async Task MigrateIdeaSubscriptionsAsync()
    {
        _logger.LogInformation("Migrating IdeaSubscriptions...");
        var subscriptions = await _dbContext.IdeaSubscriptions.ToListAsync();
        var repo = new JsonIdeaSubscriptionRepository(_jsonProvider);

        foreach (var subscription in subscriptions)
        {
            await repo.CreateAsync(subscription);
        }

        _logger.LogInformation($"Migrated {subscriptions.Count} idea subscriptions");
    }

    private async Task MigrateIdeaResultsAsync()
    {
        _logger.LogInformation("Migrating IdeaResults...");
        var results = await _dbContext.IdeaResults.ToListAsync();
        var repo = new JsonIdeaResultRepository(_jsonProvider);

        foreach (var result in results)
        {
            await repo.CreateAsync(result);
        }

        _logger.LogInformation($"Migrated {results.Count} idea results");
    }

    private async Task MigrateFormSubmissionsAsync()
    {
        _logger.LogInformation("Migrating FormSubmissions...");
        var submissions = await _dbContext.FormSubmissions.ToListAsync();
        var repo = new JsonFormSubmissionRepository(_jsonProvider);

        foreach (var submission in submissions)
        {
            await repo.CreateAsync(submission);
        }

        _logger.LogInformation($"Migrated {submissions.Count} form submissions");
    }

    private async Task MigrateWidgetApiActivitiesAsync()
    {
        _logger.LogInformation("Migrating WidgetApiActivities...");
        var activities = await _dbContext.WidgetApiActivities.ToListAsync();
        var repo = new JsonWidgetActivityRepository(_jsonProvider);

        foreach (var activity in activities)
        {
            await repo.CreateAsync(activity);
        }

        _logger.LogInformation($"Migrated {activities.Count} widget API activities");
    }
}
