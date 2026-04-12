using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;

namespace WidgetData.Infrastructure.Data;

public class DataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public DataSeeder(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task SeedAsync()
    {
        await _context.Database.EnsureCreatedAsync();

        string[] roles = { "Admin", "Manager", "Developer", "Viewer" };
        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));
        }

        if (!await _userManager.Users.AnyAsync())
        {
            var admin = new ApplicationUser
            {
                UserName = "admin@widgetdata.com",
                Email = "admin@widgetdata.com",
                DisplayName = "Admin User",
                EmailConfirmed = true,
                IsActive = true
            };
            var adminResult = await _userManager.CreateAsync(admin, "Admin@123!");
            if (adminResult.Succeeded)
                await _userManager.AddToRoleAsync(admin, "Admin");

            var manager = new ApplicationUser
            {
                UserName = "manager@widgetdata.com",
                Email = "manager@widgetdata.com",
                DisplayName = "Manager User",
                EmailConfirmed = true,
                IsActive = true
            };
            var mgResult = await _userManager.CreateAsync(manager, "Manager@123!");
            if (mgResult.Succeeded)
                await _userManager.AddToRoleAsync(manager, "Manager");

            var dev = new ApplicationUser
            {
                UserName = "dev@widgetdata.com",
                Email = "dev@widgetdata.com",
                DisplayName = "Developer User",
                EmailConfirmed = true,
                IsActive = true
            };
            var devResult = await _userManager.CreateAsync(dev, "Developer@123!");
            if (devResult.Succeeded)
                await _userManager.AddToRoleAsync(dev, "Developer");
        }

        if (!await _context.DataSources.AnyAsync())
        {
            var ds1 = new DataSource
            {
                Name = "Sales SQLite DB",
                SourceType = DataSourceType.SQLite,
                Description = "Main SQLite database for sales data",
                ConnectionString = "Data Source=sales.db",
                IsActive = true,
                CreatedBy = "system",
                LastTestedAt = DateTime.UtcNow.AddHours(-2),
                LastTestResult = "Connection successful"
            };
            var ds2 = new DataSource
            {
                Name = "Analytics REST API",
                SourceType = DataSourceType.RestApi,
                Description = "REST API endpoint for analytics data",
                ApiEndpoint = "https://api.example.com/analytics",
                ApiKey = "demo-api-key-12345",
                IsActive = true,
                CreatedBy = "system",
                LastTestedAt = DateTime.UtcNow.AddHours(-1),
                LastTestResult = "Connection successful"
            };
            var ds3 = new DataSource
            {
                Name = "Reports CSV Export",
                SourceType = DataSourceType.Csv,
                Description = "CSV files exported from reporting system",
                AdditionalConfig = "{\"path\": \"/data/reports\", \"delimiter\": \",\"}",
                IsActive = true,
                CreatedBy = "system"
            };
            _context.DataSources.AddRange(ds1, ds2, ds3);
            await _context.SaveChangesAsync();

            var w1 = new Widget
            {
                Name = "Monthly Revenue Table",
                WidgetType = WidgetType.Table,
                Description = "Shows monthly revenue breakdown by region",
                DataSourceId = ds1.Id,
                Configuration = "{\"query\": \"SELECT month, region, revenue FROM sales_monthly ORDER BY month DESC\"}",
                IsActive = true,
                CacheEnabled = true,
                CacheTtlMinutes = 30,
                CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-15),
                LastRowCount = 48
            };
            var w2 = new Widget
            {
                Name = "Sales Trend Chart",
                WidgetType = WidgetType.Chart,
                Description = "Line chart showing sales trend over last 12 months",
                DataSourceId = ds1.Id,
                Configuration = "{\"query\": \"SELECT month, total_sales FROM sales_summary\"}",
                ChartConfig = "{\"type\": \"line\", \"xAxis\": \"month\", \"yAxis\": \"total_sales\"}",
                IsActive = true,
                CacheEnabled = true,
                CacheTtlMinutes = 60,
                CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-30),
                LastRowCount = 12
            };
            var w3 = new Widget
            {
                Name = "Active Users Metric",
                WidgetType = WidgetType.Metric,
                Description = "Real-time count of active users from analytics API",
                DataSourceId = ds2.Id,
                Configuration = "{\"endpoint\": \"/users/active\", \"metric\": \"count\"}",
                IsActive = true,
                CacheEnabled = false,
                CacheTtlMinutes = 5,
                CreatedBy = "system",
                LastExecutedAt = DateTime.UtcNow.AddMinutes(-5),
                LastRowCount = 1
            };
            var w4 = new Widget
            {
                Name = "Weekly Report Summary",
                WidgetType = WidgetType.Table,
                Description = "Weekly summary from CSV report exports",
                DataSourceId = ds3.Id,
                Configuration = "{\"file\": \"weekly_summary.csv\"}",
                IsActive = false,
                CacheEnabled = false,
                CacheTtlMinutes = 15,
                CreatedBy = "system"
            };
            _context.Widgets.AddRange(w1, w2, w3, w4);
            await _context.SaveChangesAsync();

            var schedule1 = new WidgetSchedule
            {
                WidgetId = w1.Id,
                CronExpression = "0 6 * * *",
                Timezone = "UTC",
                IsEnabled = true,
                RetryOnFailure = true,
                MaxRetries = 3,
                LastRunAt = DateTime.UtcNow.AddHours(-18),
                LastRunStatus = ExecutionStatus.Success,
                NextRunAt = DateTime.UtcNow.AddHours(6)
            };
            var schedule2 = new WidgetSchedule
            {
                WidgetId = w3.Id,
                CronExpression = "*/5 * * * *",
                Timezone = "UTC",
                IsEnabled = true,
                RetryOnFailure = false,
                MaxRetries = 1,
                LastRunAt = DateTime.UtcNow.AddMinutes(-5),
                LastRunStatus = ExecutionStatus.Success,
                NextRunAt = DateTime.UtcNow.AddMinutes(5)
            };
            _context.WidgetSchedules.AddRange(schedule1, schedule2);
            await _context.SaveChangesAsync();

            // Seed sample executions for history
            var executions = new List<WidgetExecution>
            {
                new() { WidgetId = w1.Id, Status = ExecutionStatus.Success, TriggeredBy = ExecutionTrigger.Scheduler, StartedAt = DateTime.UtcNow.AddHours(-42), CompletedAt = DateTime.UtcNow.AddHours(-42).AddMilliseconds(230), ExecutionTimeMs = 230, RowCount = 48 },
                new() { WidgetId = w1.Id, Status = ExecutionStatus.Success, TriggeredBy = ExecutionTrigger.Scheduler, StartedAt = DateTime.UtcNow.AddHours(-18), CompletedAt = DateTime.UtcNow.AddHours(-18).AddMilliseconds(195), ExecutionTimeMs = 195, RowCount = 48 },
                new() { WidgetId = w2.Id, Status = ExecutionStatus.Success, TriggeredBy = ExecutionTrigger.Manual, StartedAt = DateTime.UtcNow.AddMinutes(-30), CompletedAt = DateTime.UtcNow.AddMinutes(-30).AddMilliseconds(312), ExecutionTimeMs = 312, RowCount = 12 },
                new() { WidgetId = w3.Id, Status = ExecutionStatus.Success, TriggeredBy = ExecutionTrigger.Scheduler, StartedAt = DateTime.UtcNow.AddMinutes(-5), CompletedAt = DateTime.UtcNow.AddMinutes(-5).AddMilliseconds(88), ExecutionTimeMs = 88, RowCount = 1 },
                new() { WidgetId = w4.Id, Status = ExecutionStatus.Failed, TriggeredBy = ExecutionTrigger.Manual, StartedAt = DateTime.UtcNow.AddDays(-1), CompletedAt = DateTime.UtcNow.AddDays(-1).AddMilliseconds(50), ExecutionTimeMs = 50, RowCount = 0, ErrorMessage = "File not found: weekly_summary.csv" },
            };
            _context.WidgetExecutions.AddRange(executions);
            await _context.SaveChangesAsync();
        }
    }
}
