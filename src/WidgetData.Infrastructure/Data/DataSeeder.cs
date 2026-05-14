using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
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
        await ReconcileMigrationHistoryAsync();
        await _context.Database.MigrateAsync();

        var adminSeed = await LoadAdminSeedAsync();

        await EnsureRolesAsync(adminSeed.Roles);

        var tenantBySlug = await EnsureTenantsAsync(adminSeed.Tenants);
        await EnsureUsersAsync(adminSeed.Users, tenantBySlug);

        if (!tenantBySlug.TryGetValue("demo", out var demoTenant))
            throw new InvalidOperationException("Missing required 'demo' tenant in admin seed data.");

        var salesJsonPath = Path.Combine(AppContext.BaseDirectory, "demo-sales.json");
        var courseJsonPath = Path.Combine(AppContext.BaseDirectory, "demo-course.json");
        var newsJsonPath = Path.Combine(AppContext.BaseDirectory, "demo-news.json");

        EnsureDemoJsonFile(salesJsonPath, GetSalesDemoJson());
        EnsureDemoJsonFile(courseJsonPath, GetCourseDemoJson());
        EnsureDemoJsonFile(newsJsonPath, GetNewsDemoJson());
        await EnsureDemoSourcesUseJsonFilesAsync(salesJsonPath, courseJsonPath, newsJsonPath);

        var dsMap = await EnsureDataSourcesAsync(adminSeed.DataSources, salesJsonPath, courseJsonPath, newsJsonPath);
        var groupMap = await EnsureWidgetGroupsAsync(adminSeed.WidgetGroups);
        var widgetMap = await EnsureWidgetsAsync(adminSeed.Widgets, dsMap);
        await EnsureWidgetGroupMembersAsync(adminSeed.WidgetGroupMembers, groupMap, widgetMap);
        await EnsureWidgetSchedulesAsync(adminSeed.WidgetSchedules, widgetMap);
        await EnsureWidgetExecutionsAsync(adminSeed.WidgetExecutionPatterns, groupMap, widgetMap);
        await EnsureFormSubmissionsAsync(adminSeed.FormSubmissions, widgetMap);
        var targetMap = await EnsureDeliveryTargetsAsync(adminSeed.DeliveryTargets, widgetMap);
        await EnsureDeliveryExecutionsAsync(adminSeed.DeliveryExecutions, targetMap);
        await EnsureWidgetConfigArchivesAsync(adminSeed.ConfigArchives, widgetMap);
        await EnsureWidgetApiActivitiesAsync(widgetMap);
        await EnsureIdeaPostsAsync(adminSeed.IdeaPosts, widgetMap);
        await EnsurePagesAsync(adminSeed.Pages, tenantBySlug, widgetMap);
        await SeedAuditLogsAsync(adminSeed.AuditLogs);
    }

    private async Task EnsureDemoSourcesUseJsonFilesAsync(string salesJsonPath, string courseJsonPath, string newsJsonPath)
    {
        var demoSources = await _context.DataSources
            .Where(ds =>
                ds.Name == "Cửa hàng - Sales DB" ||
                ds.Name == "Cửa hàng - Sales Data" ||
                ds.Name == "EduViet - Course DB" ||
                ds.Name == "EduViet - Course Data" ||
                ds.Name == "VietNews - News DB" ||
                ds.Name == "VietNews - News Data")
            .ToListAsync();

        if (demoSources.Count == 0) return;

        foreach (var ds in demoSources)
        {
            if (ds.Name.StartsWith("Cửa hàng", StringComparison.OrdinalIgnoreCase))
            {
                ds.Name = "Cửa hàng - Sales Data";
                ds.SourceType = DataSourceType.Json;
                ds.Description = "Dữ liệu JSON demo cho bán hàng, khách hàng, sản phẩm và thanh toán";
                ds.FileStoragePath = salesJsonPath;
                ds.OriginalFileName = "demo-sales.json";
                ds.StoredFileName = "demo-sales.json";
                ds.FileContentType = "application/json";
                ds.ConnectionString = null;
            }
            else if (ds.Name.StartsWith("EduViet", StringComparison.OrdinalIgnoreCase))
            {
                ds.Name = "EduViet - Course Data";
                ds.SourceType = DataSourceType.Json;
                ds.Description = "Dữ liệu JSON demo cho nền tảng học trực tuyến EduViet";
                ds.FileStoragePath = courseJsonPath;
                ds.OriginalFileName = "demo-course.json";
                ds.StoredFileName = "demo-course.json";
                ds.FileContentType = "application/json";
                ds.ConnectionString = null;
            }
            else if (ds.Name.StartsWith("VietNews", StringComparison.OrdinalIgnoreCase))
            {
                ds.Name = "VietNews - News Data";
                ds.SourceType = DataSourceType.Json;
                ds.Description = "Dữ liệu JSON demo cho cổng tin tức VietNews";
                ds.FileStoragePath = newsJsonPath;
                ds.OriginalFileName = "demo-news.json";
                ds.StoredFileName = "demo-news.json";
                ds.FileContentType = "application/json";
                ds.ConnectionString = null;
            }

            ds.FileSizeBytes = new FileInfo(ds.FileStoragePath!).Length;
            ds.FileUploadedAt = DateTime.UtcNow;
            ds.FileUploadedBy = "system";
            ds.LastTestedAt = DateTime.UtcNow;
            ds.LastTestResult = "Connection successful";
        }

        await _context.SaveChangesAsync();
    }

    private static void EnsureDemoJsonFile(string filePath, string content)
    {
        if (File.Exists(filePath)) return;
        File.WriteAllText(filePath, content);
    }

    private static string GetSalesDemoJson() => """
[
  { "value": 125000000, "month": "2026-01", "revenue": 125000000, "category": "Điện thoại", "quantity": 420, "status": "completed", "payment_method": "bank_transfer", "amount": 125000000, "label": "Điện thoại", "city": "Hà Nội", "day": "01/15" },
  { "value": 119500000, "month": "2026-02", "revenue": 119500000, "category": "Laptop", "quantity": 250, "status": "completed", "payment_method": "card", "amount": 119500000, "label": "Laptop", "city": "TP. Hồ Chí Minh", "day": "02/15" },
  { "value": 132800000, "month": "2026-03", "revenue": 132800000, "category": "Phụ kiện", "quantity": 960, "status": "completed", "payment_method": "cash", "amount": 132800000, "label": "Phụ kiện", "city": "Đà Nẵng", "day": "03/15" },
  { "value": 141200000, "month": "2026-04", "revenue": 141200000, "category": "Máy tính bảng", "quantity": 340, "status": "completed", "payment_method": "e_wallet", "amount": 141200000, "label": "Máy tính bảng", "city": "Cần Thơ", "day": "04/15" },
  { "value": 138400000, "month": "2026-05", "revenue": 138400000, "category": "Thiết bị mạng", "quantity": 220, "status": "completed", "payment_method": "bank_transfer", "amount": 138400000, "label": "Thiết bị mạng", "city": "Hải Phòng", "day": "05/15" }
]
""";

    private static string GetCourseDemoJson() => """
[
  { "value": 3850, "month": "2026-01", "revenue": 385000000, "category": "Lập trình", "quantity": 520, "status": "active", "payment_method": "card", "amount": 385000000, "label": "Lập trình", "city": "Hà Nội", "day": "01/20" },
  { "value": 4020, "month": "2026-02", "revenue": 402000000, "category": "Thiết kế", "quantity": 470, "status": "active", "payment_method": "bank_transfer", "amount": 402000000, "label": "Thiết kế", "city": "TP. Hồ Chí Minh", "day": "02/20" },
  { "value": 4175, "month": "2026-03", "revenue": 417500000, "category": "Marketing", "quantity": 560, "status": "active", "payment_method": "e_wallet", "amount": 417500000, "label": "Marketing", "city": "Đà Nẵng", "day": "03/20" },
  { "value": 4380, "month": "2026-04", "revenue": 438000000, "category": "Data", "quantity": 610, "status": "active", "payment_method": "card", "amount": 438000000, "label": "Data", "city": "Huế", "day": "04/20" },
  { "value": 4510, "month": "2026-05", "revenue": 451000000, "category": "AI", "quantity": 645, "status": "active", "payment_method": "bank_transfer", "amount": 451000000, "label": "AI", "city": "Nha Trang", "day": "05/20" }
]
""";

    private static string GetNewsDemoJson() => """
[
  { "value": 188000, "month": "2026-01", "revenue": 188000, "category": "Thời sự", "quantity": 95, "status": "published", "payment_method": "organic", "amount": 188000, "label": "Thời sự", "city": "Hà Nội", "day": "01/10" },
  { "value": 201500, "month": "2026-02", "revenue": 201500, "category": "Kinh doanh", "quantity": 88, "status": "published", "payment_method": "social", "amount": 201500, "label": "Kinh doanh", "city": "TP. Hồ Chí Minh", "day": "02/10" },
  { "value": 213200, "month": "2026-03", "revenue": 213200, "category": "Công nghệ", "quantity": 102, "status": "published", "payment_method": "search", "amount": 213200, "label": "Công nghệ", "city": "Đà Nẵng", "day": "03/10" },
  { "value": 209700, "month": "2026-04", "revenue": 209700, "category": "Giáo dục", "quantity": 91, "status": "published", "payment_method": "direct", "amount": 209700, "label": "Giáo dục", "city": "Cần Thơ", "day": "04/10" },
  { "value": 227900, "month": "2026-05", "revenue": 227900, "category": "Thể thao", "quantity": 110, "status": "published", "payment_method": "organic", "amount": 227900, "label": "Thể thao", "city": "Hải Phòng", "day": "05/10" }
]
""";

    private static readonly JsonSerializerOptions SeedJsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly string AdminSeedDirectory = Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "admin");

    private async Task<AdminSeedData> LoadAdminSeedAsync()
    {
        var roles       = await LoadSeedFileAsync<List<string>>("roles.json");
        var tenants     = await LoadSeedFileAsync<List<AdminTenantSeed>>("tenants.json");
        var users       = await LoadSeedFileAsync<List<AdminUserSeed>>("users.json");
        var auditLogs   = await LoadSeedFileAsync<List<AdminAuditLogSeed>>("audit-logs.json");

        var dataSources       = await LoadSeedFileAsync<List<DataSourceSeed>>("data-sources.json");
        var widgetGroups      = await LoadSeedFileAsync<List<WidgetGroupSeed>>("widget-groups.json");
        var widgets           = await LoadSeedFileAsync<List<WidgetSeed>>("widgets.json");
        var groupMembers      = await LoadSeedFileAsync<List<WidgetGroupMemberSeed>>("widget-group-members.json");
        var schedules         = await LoadSeedFileAsync<List<WidgetScheduleSeed>>("widget-schedules.json");
        var execPatterns      = await LoadSeedFileAsync<List<WidgetExecutionPatternSeed>>("widget-executions.json");
        var formSubmissions   = await LoadSeedFileAsync<List<FormSubmissionSeed>>("form-submissions.json");
        var deliveryTargets   = await LoadSeedFileAsync<List<DeliveryTargetSeed>>("delivery-targets.json");
        var deliveryExecs     = await LoadSeedFileAsync<List<DeliveryExecutionSeed>>("delivery-executions.json");
        var configArchives    = await LoadSeedFileAsync<List<WidgetConfigArchiveSeed>>("widget-config-archives.json");
        var ideaPosts         = await LoadSeedFileAsync<IdeaSeedData>("idea-posts.json");
        var pages             = await LoadSeedFileAsync<List<PageSeed>>("pages.json");

        var normalizedRoles = NormalizeRoles(roles);
        var normalizedUsers = users
            .Select(user => user with { Roles = NormalizeRoles(user.Roles) })
            .ToList();

        return new AdminSeedData(
            normalizedRoles, tenants, normalizedUsers, auditLogs,
            dataSources, widgetGroups, widgets, groupMembers, schedules,
            execPatterns, formSubmissions, deliveryTargets, deliveryExecs,
            configArchives, ideaPosts, pages);
    }


    private static async Task<T> LoadSeedFileAsync<T>(string fileName) where T : class
    {
        var filePath = Path.Combine(AdminSeedDirectory, fileName);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Seed file not found: {filePath}", filePath);

        await using var stream = File.OpenRead(filePath);
        var data = await JsonSerializer.DeserializeAsync<T>(stream, SeedJsonOptions);
        return data ?? throw new InvalidOperationException($"Seed file '{fileName}' is empty or invalid JSON.");
    }

    private static List<string> NormalizeRoles(IEnumerable<string> roles)
        => roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private async Task EnsureRolesAsync(IReadOnlyCollection<string> roles)
    {
        foreach (var role in roles.Where(r => !string.IsNullOrWhiteSpace(r)))
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private async Task<Dictionary<string, Tenant>> EnsureTenantsAsync(IReadOnlyCollection<AdminTenantSeed> tenantSeeds)
    {
        var slugs = tenantSeeds
            .Select(t => t.Slug)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingTenants = await _context.Tenants
            .Where(t => slugs.Contains(t.Slug))
            .ToListAsync();

        var tenantBySlug = existingTenants.ToDictionary(t => t.Slug, StringComparer.OrdinalIgnoreCase);

        foreach (var seed in tenantSeeds)
        {
            if (string.IsNullOrWhiteSpace(seed.Slug))
                continue;

            if (tenantBySlug.TryGetValue(seed.Slug, out var existing))
            {
                // Keep existing tenant metadata untouched; seed only creates missing tenants.
                continue;
            }

            var tenant = new Tenant
            {
                Name = seed.Name,
                Slug = seed.Slug,
                IsActive = seed.IsActive,
                Plan = seed.Plan,
                ContactEmail = seed.ContactEmail,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tenants.Add(tenant);
            tenantBySlug[tenant.Slug] = tenant;
        }

        await _context.SaveChangesAsync();

        return await _context.Tenants
            .Where(t => slugs.Contains(t.Slug))
            .ToDictionaryAsync(t => t.Slug, StringComparer.OrdinalIgnoreCase);
    }

    private async Task EnsureUsersAsync(IReadOnlyCollection<AdminUserSeed> userSeeds, IReadOnlyDictionary<string, Tenant> tenantBySlug)
    {
        foreach (var seed in userSeeds)
        {
            if (string.IsNullOrWhiteSpace(seed.Email) || string.IsNullOrWhiteSpace(seed.Password))
                continue;

            var existingUser = await _userManager.FindByEmailAsync(seed.Email);
            if (existingUser == null)
            {
                var user = new ApplicationUser
                {
                    UserName = seed.Email,
                    Email = seed.Email,
                    DisplayName = seed.DisplayName,
                    EmailConfirmed = true,
                    IsActive = seed.IsActive,
                    TenantId = ResolveTenantId(seed.TenantSlug, tenantBySlug),
                    LastLoginAt = seed.LastLoginDaysAgo.HasValue ? DateTime.UtcNow.AddDays(-seed.LastLoginDaysAgo.Value) : null
                };

                var createResult = await _userManager.CreateAsync(user, seed.Password);
                if (!createResult.Succeeded)
                    continue;

                existingUser = user;
            }

            foreach (var role in seed.Roles.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!await _userManager.IsInRoleAsync(existingUser, role))
                    await _userManager.AddToRoleAsync(existingUser, role);
            }
        }
    }
    // ─────────────────────────────────────────────────────────────────────────
    // JSON-driven Ensure methods
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<Dictionary<string, DataSource>> EnsureDataSourcesAsync(
        IReadOnlyCollection<DataSourceSeed> seeds,
        string salesJsonPath, string courseJsonPath, string newsJsonPath)
    {
        var pathMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["demo-sales.json"]  = salesJsonPath,
            ["demo-course.json"] = courseJsonPath,
            ["demo-news.json"]   = newsJsonPath
        };

        foreach (var seed in seeds)
        {
            if (await _context.DataSources.AnyAsync(ds => ds.Name == seed.Name))
                continue;

            string? filePath = null;
            if (seed.FileNameInBaseDirectory is not null
                && pathMap.TryGetValue(seed.FileNameInBaseDirectory, out var p))
                filePath = p;

            var ds = new DataSource
            {
                Name             = seed.Name,
                SourceType       = Enum.Parse<DataSourceType>(seed.SourceType),
                Description      = seed.Description,
                ApiEndpoint      = seed.ApiEndpoint,
                ApiKey           = seed.ApiKey,
                IsActive         = seed.IsActive,
                CreatedBy        = "system",
                LastTestedAt     = seed.LastTestedHoursAgo.HasValue
                                       ? DateTime.UtcNow.AddHours(-seed.LastTestedHoursAgo.Value) : DateTime.UtcNow,
                LastTestResult   = "Connection successful",
                FileStoragePath  = filePath,
                OriginalFileName = seed.FileNameInBaseDirectory,
                StoredFileName   = seed.FileNameInBaseDirectory,
                FileContentType  = filePath is not null ? "application/json" : null,
                FileSizeBytes    = filePath is not null && File.Exists(filePath) ? new FileInfo(filePath).Length : null,
                FileUploadedAt   = filePath is not null ? DateTime.UtcNow : null,
                FileUploadedBy   = filePath is not null ? "system" : null
            };
            _context.DataSources.Add(ds);
        }
        await _context.SaveChangesAsync();

        var names = seeds.Select(s => s.Name).ToList();
        return await _context.DataSources
            .Where(ds => names.Contains(ds.Name))
            .ToDictionaryAsync(ds => ds.Name);
    }

    private async Task<Dictionary<string, WidgetGroup>> EnsureWidgetGroupsAsync(
        IReadOnlyCollection<WidgetGroupSeed> seeds)
    {
        foreach (var seed in seeds)
        {
            if (await _context.WidgetGroups.AnyAsync(g => g.Name == seed.Name))
                continue;

            _context.WidgetGroups.Add(new WidgetGroup
            {
                Name      = seed.Name,
                Description = seed.Description,
                IsActive  = seed.IsActive,
                CreatedBy = "system"
            });
        }
        await _context.SaveChangesAsync();

        var names = seeds.Select(s => s.Name).ToList();
        return await _context.WidgetGroups
            .Where(g => names.Contains(g.Name))
            .ToDictionaryAsync(g => g.Name);
    }

    private async Task<Dictionary<string, Widget>> EnsureWidgetsAsync(
        IReadOnlyCollection<WidgetSeed> seeds,
        IReadOnlyDictionary<string, DataSource> dsMap)
    {
        foreach (var seed in seeds)
        {
            if (await _context.Widgets.IgnoreQueryFilters().AnyAsync(w => w.Name == seed.Name))
                continue;

            if (!dsMap.TryGetValue(seed.DataSourceName, out var ds)) continue;

            string? configStr    = seed.Configuration.HasValue
                                       ? JsonSerializer.Serialize(seed.Configuration.Value) : null;
            string? chartCfgStr  = seed.ChartConfig.HasValue
                                       ? JsonSerializer.Serialize(seed.ChartConfig.Value) : null;

            _context.Widgets.Add(new Widget
            {
                Name               = seed.Name,
                FriendlyLabel      = seed.FriendlyLabel,
                HelpText           = seed.HelpText,
                WidgetType         = Enum.Parse<WidgetType>(seed.WidgetType),
                Description        = seed.Description,
                DataSourceId       = ds.Id,
                Configuration      = configStr,
                ChartConfig        = chartCfgStr,
                IsActive           = seed.IsActive,
                CacheEnabled       = seed.CacheEnabled,
                CacheTtlMinutes    = seed.CacheTtlMinutes,
                CreatedBy          = "system",
                LastExecutedAt     = seed.LastExecutedMinutesAgo.HasValue
                                         ? DateTime.UtcNow.AddMinutes(-seed.LastExecutedMinutesAgo.Value) : null,
                LastRowCount       = seed.LastRowCount
            });
        }
        await _context.SaveChangesAsync();

        var names = seeds.Select(s => s.Name).ToList();
        return await _context.Widgets.IgnoreQueryFilters()
            .Where(w => names.Contains(w.Name))
            .ToDictionaryAsync(w => w.Name);
    }

    private async Task EnsureWidgetGroupMembersAsync(
        IReadOnlyCollection<WidgetGroupMemberSeed> seeds,
        IReadOnlyDictionary<string, WidgetGroup> groupMap,
        IReadOnlyDictionary<string, Widget> widgetMap)
    {
        foreach (var seed in seeds)
        {
            if (!groupMap.TryGetValue(seed.GroupName, out var group)) continue;

            foreach (var widgetName in seed.WidgetNames)
            {
                if (!widgetMap.TryGetValue(widgetName, out var widget)) continue;

                var exists = await _context.WidgetGroupMembers
                    .AnyAsync(m => m.WidgetGroupId == group.Id && m.WidgetId == widget.Id);
                if (!exists)
                    _context.WidgetGroupMembers.Add(new WidgetGroupMember
                        { WidgetGroupId = group.Id, WidgetId = widget.Id });
            }
        }
        await _context.SaveChangesAsync();
    }

    private async Task EnsureWidgetSchedulesAsync(
        IReadOnlyCollection<WidgetScheduleSeed> seeds,
        IReadOnlyDictionary<string, Widget> widgetMap)
    {
        foreach (var seed in seeds)
        {
            if (!widgetMap.TryGetValue(seed.WidgetName, out var widget)) continue;

            var exists = await _context.WidgetSchedules
                .AnyAsync(s => s.WidgetId == widget.Id && s.CronExpression == seed.CronExpression);
            if (exists) continue;

            var lastRunAt = seed.HoursAgoLastRun.HasValue
                                ? DateTime.UtcNow.AddHours(-seed.HoursAgoLastRun.Value)
                                : seed.MinutesAgoLastRun.HasValue
                                    ? DateTime.UtcNow.AddMinutes(-seed.MinutesAgoLastRun.Value)
                                    : (DateTime?)null;

            var nextRunAt = seed.HoursUntilNextRun.HasValue
                                ? DateTime.UtcNow.AddHours(seed.HoursUntilNextRun.Value)
                                : seed.MinutesUntilNextRun.HasValue
                                    ? DateTime.UtcNow.AddMinutes(seed.MinutesUntilNextRun.Value)
                                    : (DateTime?)null;

            _context.WidgetSchedules.Add(new WidgetSchedule
            {
                WidgetId        = widget.Id,
                CronExpression  = seed.CronExpression,
                Timezone        = seed.Timezone,
                IsEnabled       = seed.IsEnabled,
                RetryOnFailure  = seed.RetryOnFailure,
                MaxRetries      = seed.MaxRetries,
                LastRunAt       = lastRunAt,
                LastRunStatus   = lastRunAt.HasValue
                                      ? Enum.Parse<ExecutionStatus>(seed.LastRunStatus) : null,
                NextRunAt       = nextRunAt
            });
        }
        await _context.SaveChangesAsync();
    }

    private async Task EnsureWidgetExecutionsAsync(
        IReadOnlyCollection<WidgetExecutionPatternSeed> patterns,
        IReadOnlyDictionary<string, WidgetGroup> groupMap,
        IReadOnlyDictionary<string, Widget> widgetMap)
    {
        if (await _context.WidgetExecutions.AnyAsync()) return;

        var executions = new List<WidgetExecution>();

        foreach (var pattern in patterns)
        {
            var groupIds = pattern.GroupNames
                .Where(gn => groupMap.ContainsKey(gn))
                .Select(gn => groupMap[gn].Id)
                .ToList();

            var widgetIds = await _context.WidgetGroupMembers
                .Where(m => groupIds.Contains(m.WidgetGroupId))
                .Select(m => m.WidgetId)
                .Distinct()
                .ToListAsync();

            var widgets = await _context.Widgets.IgnoreQueryFilters()
                .Where(w => widgetIds.Contains(w.Id))
                .ToListAsync();

            var excludeTypes = pattern.ExcludeWidgetTypes
                .Select(t => Enum.Parse<WidgetType>(t))
                .ToHashSet();

            foreach (var widget in widgets.Where(w => !excludeTypes.Contains(w.WidgetType)))
            {
                foreach (var entry in pattern.Entries)
                {
                    var startedAt = DateTime.UtcNow.AddHours(-entry.HoursAgo);
                    executions.Add(new WidgetExecution
                    {
                        WidgetId       = widget.Id,
                        Status         = ExecutionStatus.Success,
                        TriggeredBy    = Enum.Parse<ExecutionTrigger>(entry.TriggeredBy),
                        StartedAt      = startedAt,
                        CompletedAt    = startedAt.AddMilliseconds(entry.ExecutionTimeMs),
                        ExecutionTimeMs = entry.ExecutionTimeMs,
                        RowCount       = widget.LastRowCount ?? 0
                    });
                }
            }
        }

        _context.WidgetExecutions.AddRange(executions);
        await _context.SaveChangesAsync();
    }

    private async Task EnsureFormSubmissionsAsync(
        IReadOnlyCollection<FormSubmissionSeed> seeds,
        IReadOnlyDictionary<string, Widget> widgetMap)
    {
        if (await _context.FormSubmissions.AnyAsync()) return;

        var submissions = new List<FormSubmission>();
        foreach (var seed in seeds)
        {
            if (!widgetMap.TryGetValue(seed.WidgetName, out var widget)) continue;

            submissions.Add(new FormSubmission
            {
                WidgetId    = widget.Id,
                Data        = seed.Data,
                IpAddress   = seed.IpAddress,
                SubmittedAt = seed.HoursAgo.HasValue
                                  ? DateTime.UtcNow.AddHours(-seed.HoursAgo.Value)
                                  : DateTime.UtcNow.AddDays(-seed.DaysAgo)
            });
        }

        _context.FormSubmissions.AddRange(submissions);
        await _context.SaveChangesAsync();
    }

    private async Task<Dictionary<string, DeliveryTarget>> EnsureDeliveryTargetsAsync(
        IReadOnlyCollection<DeliveryTargetSeed> seeds,
        IReadOnlyDictionary<string, Widget> widgetMap)
    {
        foreach (var seed in seeds)
        {
            if (await _context.DeliveryTargets.AnyAsync(dt => dt.Name == seed.Name)) continue;
            if (!widgetMap.TryGetValue(seed.WidgetName, out var widget)) continue;

            string? cfgStr = seed.Configuration.HasValue
                                 ? JsonSerializer.Serialize(seed.Configuration.Value) : null;

            _context.DeliveryTargets.Add(new DeliveryTarget
            {
                WidgetId      = widget.Id,
                Name          = seed.Name,
                Type          = Enum.Parse<DeliveryType>(seed.Type),
                Configuration = cfgStr,
                IsEnabled     = seed.IsEnabled,
                CreatedBy     = "system"
            });
        }
        await _context.SaveChangesAsync();

        var names = seeds.Select(s => s.Name).ToList();
        return await _context.DeliveryTargets
            .Where(dt => names.Contains(dt.Name))
            .ToDictionaryAsync(dt => dt.Name);
    }

    private async Task EnsureDeliveryExecutionsAsync(
        IReadOnlyCollection<DeliveryExecutionSeed> seeds,
        IReadOnlyDictionary<string, DeliveryTarget> targetMap)
    {
        if (await _context.DeliveryExecutions.AnyAsync()) return;

        var executions = new List<DeliveryExecution>();
        foreach (var seed in seeds)
        {
            if (!targetMap.TryGetValue(seed.DeliveryTargetName, out var target)) continue;

            executions.Add(new DeliveryExecution
            {
                DeliveryTargetId = target.Id,
                Status           = Enum.Parse<ExecutionStatus>(seed.Status),
                Message          = seed.Message,
                TriggeredBy      = seed.TriggeredBy,
                ExecutedAt       = seed.HoursAgo.HasValue
                                       ? DateTime.UtcNow.AddHours(-seed.HoursAgo.Value)
                                       : DateTime.UtcNow.AddDays(-seed.DaysAgo)
            });
        }

        _context.DeliveryExecutions.AddRange(executions);
        await _context.SaveChangesAsync();
    }

    private async Task EnsureWidgetConfigArchivesAsync(
        IReadOnlyCollection<WidgetConfigArchiveSeed> seeds,
        IReadOnlyDictionary<string, Widget> widgetMap)
    {
        if (await _context.WidgetConfigArchives.AnyAsync()) return;

        var archives = new List<WidgetConfigArchive>();
        foreach (var seed in seeds)
        {
            if (!widgetMap.TryGetValue(seed.WidgetName, out var widget)) continue;

            archives.Add(new WidgetConfigArchive
            {
                WidgetId      = widget.Id,
                Configuration = seed.Configuration,
                ChartConfig   = seed.ChartConfig,
                HtmlTemplate  = seed.HtmlTemplate,
                Note          = seed.Note,
                TriggerSource = seed.TriggerSource,
                ArchivedBy    = seed.ArchivedBy,
                ArchivedAt    = DateTime.UtcNow.AddDays(-seed.DaysAgo)
            });
        }

        _context.WidgetConfigArchives.AddRange(archives);
        await _context.SaveChangesAsync();
    }

    private async Task EnsureWidgetApiActivitiesAsync(IReadOnlyDictionary<string, Widget> widgetMap)
    {
        if (await _context.WidgetApiActivities.AnyAsync()) return;

        var activityWidgetNames = new[]
        {
            "total_revenue_metric", "total_orders_metric", "avg_order_value_metric", "total_customers_metric",
            "monthly_revenue_trend", "revenue_by_category_chart", "order_status_summary",
            "top_products_by_revenue", "product_sales_by_category_chart", "low_stock_products", "daily_orders_last30",
            "top_customers_by_revenue", "customers_by_city_chart", "recent_orders_table",
            "payment_method_distribution", "daily_payment_trend", "payment_summary_by_method", "failed_refunded_payments"
        };

        var actRand = new Random(77);
        var activities = new List<WidgetApiActivity>();
        foreach (var name in activityWidgetNames)
        {
            if (!widgetMap.TryGetValue(name, out var widget)) continue;
            for (int i = 0; i < 40; i++)
            {
                activities.Add(new WidgetApiActivity
                {
                    WidgetId      = widget.Id,
                    ApiEndpoint   = $"/api/widgets/{widget.Id}/execute",
                    UserId        = actRand.Next(0, 4) == 0 ? null : $"user-demo-{actRand.Next(1, 5)}",
                    CalledAt      = DateTime.UtcNow.AddHours(-actRand.Next(1, 720)),
                    ResponseTimeMs = actRand.Next(40, 650),
                    StatusCode    = actRand.Next(0, 12) == 0 ? 500 : (actRand.Next(0, 20) == 0 ? 429 : 200)
                });
            }
        }

        _context.WidgetApiActivities.AddRange(activities);
        await _context.SaveChangesAsync();
    }

    private async Task EnsureIdeaPostsAsync(
        IdeaSeedData seed,
        IReadOnlyDictionary<string, Widget> widgetMap)
    {
        if (await _context.IdeaPosts.AnyAsync()) return;

        // Subscriptions
        var subMap = new Dictionary<string, IdeaSubscription>(StringComparer.OrdinalIgnoreCase);
        foreach (var subSeed in seed.Subscriptions)
        {
            if (!widgetMap.TryGetValue(subSeed.WidgetName, out var widget)) continue;

            var sub = new IdeaSubscription
            {
                WidgetId    = widget.Id,
                Name        = subSeed.Name,
                LabelFilter = subSeed.LabelFilter,
                IsActive    = subSeed.IsActive,
                CreatedBy   = "system"
            };
            _context.IdeaSubscriptions.Add(sub);
            subMap[sub.Name] = sub;
        }
        await _context.SaveChangesAsync();

        // Posts and results
        foreach (var postSeed in seed.Posts)
        {
            if (!widgetMap.TryGetValue(postSeed.WidgetName, out var widget)) continue;

            var post = new IdeaPost
            {
                WidgetId    = widget.Id,
                Title       = postSeed.Title,
                Content     = postSeed.Content,
                Labels      = postSeed.Labels,
                Status      = postSeed.Status,
                CreatedBy   = postSeed.CreatedBy,
                CreatedAt   = DateTime.UtcNow.AddDays(-postSeed.DaysAgo),
                ProcessedAt = postSeed.ProcessedDaysAgo.HasValue
                                  ? DateTime.UtcNow.AddDays(-postSeed.ProcessedDaysAgo.Value) : null
            };
            _context.IdeaPosts.Add(post);
            await _context.SaveChangesAsync();

            foreach (var resSeed in postSeed.Results)
            {
                if (!subMap.TryGetValue(resSeed.SubscriptionName, out var sub)) continue;

                _context.IdeaResults.Add(new IdeaResult
                {
                    IdeaPostId           = post.Id,
                    IdeaSubscriptionId   = sub.Id,
                    ResultContent        = resSeed.ResultContent,
                    Status               = resSeed.Status,
                    CreatedAt            = DateTime.UtcNow.AddDays(-resSeed.DaysAgo)
                });
            }
        }
        await _context.SaveChangesAsync();
    }

    private async Task EnsurePagesAsync(
        IReadOnlyCollection<PageSeed> seeds,
        IReadOnlyDictionary<string, Tenant> tenantBySlug,
        IReadOnlyDictionary<string, Widget> widgetMap)
    {
        foreach (var seed in seeds)
        {
            if (!tenantBySlug.TryGetValue(seed.TenantSlug, out var tenant)) continue;

            if (await _context.Pages.IgnoreQueryFilters()
                    .AnyAsync(p => p.TenantId == tenant.Id && p.Slug == seed.Slug))
                continue;

            var page = new Page
            {
                TenantId    = tenant.Id,
                Title       = seed.Title,
                Slug        = seed.Slug,
                Description = seed.Description,
                IsActive    = true,
                CreatedBy   = "system"
            };
            _context.Pages.Add(page);
            await _context.SaveChangesAsync();

            var widgetNames = seed.WidgetNames;
            var widgets = await _context.Widgets.IgnoreQueryFilters()
                .Where(w => widgetNames.Contains(w.Name))
                .ToListAsync();

            var nameIndex = widgetNames
                .Select((name, idx) => (name, idx))
                .ToDictionary(x => x.name, x => x.idx);
            widgets = widgets
                .OrderBy(w => nameIndex.GetValueOrDefault(w.Name, int.MaxValue))
                .ToList();

            for (int i = 0; i < widgets.Count; i++)
                _context.PageWidgets.Add(new PageWidget
                {
                    PageId   = page.Id,
                    WidgetId = widgets[i].Id,
                    Position = i,
                    Width    = i < seed.NarrowWidgetCount ? 3 : 6
                });

            await _context.SaveChangesAsync();
        }
    }

    private async Task SeedAuditLogsAsync(IReadOnlyCollection<AdminAuditLogSeed> auditLogSeeds)
    {
        if (await _context.AuditLogs.AnyAsync()) return;

        var now = DateTime.UtcNow;
        var rand = new Random(42);

        var logs = auditLogSeeds.Select(seed => new AuditLog
        {
            Action = seed.Action,
            EntityType = seed.EntityType,
            EntityId = seed.EntityId,
            UserId = seed.UserId,
            UserEmail = seed.UserEmail,
            OldValues = seed.OldValues,
            NewValues = seed.NewValues,
            IpAddress = seed.RandomIp ? CreateRandomIp(rand) : seed.IpAddress,
            UserAgent = seed.UserAgent,
            Timestamp = now.AddDays(-seed.DaysAgo),
            Notes = seed.Notes
        }).ToList();

        _context.AuditLogs.AddRange(logs);
        await _context.SaveChangesAsync();
    }

    private static string CreateRandomIp(Random random)
        => $"{random.Next(1, 255)}.{random.Next(0, 255)}.{random.Next(0, 255)}.{random.Next(1, 255)}";

    private static int? ResolveTenantId(string? tenantSlug, IReadOnlyDictionary<string, Tenant> tenantBySlug)
    {
        if (string.IsNullOrWhiteSpace(tenantSlug))
            return null;

        return tenantBySlug.TryGetValue(tenantSlug, out var tenant) ? tenant.Id : null;
    }

    private sealed record AdminSeedData(
        List<string> Roles,
        List<AdminTenantSeed> Tenants,
        List<AdminUserSeed> Users,
        List<AdminAuditLogSeed> AuditLogs,
        List<DataSourceSeed> DataSources,
        List<WidgetGroupSeed> WidgetGroups,
        List<WidgetSeed> Widgets,
        List<WidgetGroupMemberSeed> WidgetGroupMembers,
        List<WidgetScheduleSeed> WidgetSchedules,
        List<WidgetExecutionPatternSeed> WidgetExecutionPatterns,
        List<FormSubmissionSeed> FormSubmissions,
        List<DeliveryTargetSeed> DeliveryTargets,
        List<DeliveryExecutionSeed> DeliveryExecutions,
        List<WidgetConfigArchiveSeed> ConfigArchives,
        IdeaSeedData IdeaPosts,
        List<PageSeed> Pages);


    private sealed record AdminTenantSeed
    {
        public string Name { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public bool IsActive { get; init; } = true;
        public string Plan { get; init; } = "free";
        public string? ContactEmail { get; init; }
    }

    private sealed record AdminUserSeed
    {
        public string Email { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public bool IsActive { get; init; } = true;
        public string? TenantSlug { get; init; }
        public int? LastLoginDaysAgo { get; init; }
        public List<string> Roles { get; init; } = [];
    }

    private sealed record AdminAuditLogSeed
    {
        public string Action { get; init; } = string.Empty;
        public string? EntityType { get; init; }
        public string? EntityId { get; init; }
        public string? UserId { get; init; }
        public string? UserEmail { get; init; }
        public string? OldValues { get; init; }
        public string? NewValues { get; init; }
        public string? IpAddress { get; init; }
        public bool RandomIp { get; init; }
        public string? UserAgent { get; init; }
        public int DaysAgo { get; init; }
        public string? Notes { get; init; }
    }

    private sealed record DataSourceSeed
    {
        public string Name { get; init; } = string.Empty;
        public string SourceType { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? ApiEndpoint { get; init; }
        public string? ApiKey { get; init; }
        public string? FileNameInBaseDirectory { get; init; }
        public bool IsActive { get; init; } = true;
        public double? LastTestedHoursAgo { get; init; }
    }

    private sealed record WidgetGroupSeed
    {
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public bool IsActive { get; init; } = true;
    }

    private sealed record WidgetSeed
    {
        public string Name { get; init; } = string.Empty;
        public string FriendlyLabel { get; init; } = string.Empty;
        public string? HelpText { get; init; }
        public string WidgetType { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string DataSourceName { get; init; } = string.Empty;
        public System.Text.Json.JsonElement? Configuration { get; init; }
        public System.Text.Json.JsonElement? ChartConfig { get; init; }
        public bool IsActive { get; init; } = true;
        public bool CacheEnabled { get; init; } = false;
        public int CacheTtlMinutes { get; init; } = 15;
        public int? LastExecutedMinutesAgo { get; init; }
        public int? LastRowCount { get; init; }
    }

    private sealed record WidgetGroupMemberSeed
    {
        public string GroupName { get; init; } = string.Empty;
        public List<string> WidgetNames { get; init; } = [];
    }

    private sealed record WidgetScheduleSeed
    {
        public string WidgetName { get; init; } = string.Empty;
        public string CronExpression { get; init; } = string.Empty;
        public string Timezone { get; init; } = "UTC";
        public bool IsEnabled { get; init; } = true;
        public bool RetryOnFailure { get; init; } = false;
        public int MaxRetries { get; init; } = 3;
        public double? HoursAgoLastRun { get; init; }
        public int? MinutesAgoLastRun { get; init; }
        public double? HoursUntilNextRun { get; init; }
        public int? MinutesUntilNextRun { get; init; }
        public string LastRunStatus { get; init; } = "Success";
    }

    private sealed record WidgetExecutionPatternSeed
    {
        public List<string> GroupNames { get; init; } = [];
        public List<string> ExcludeWidgetTypes { get; init; } = [];
        public List<WidgetExecutionPatternEntry> Entries { get; init; } = [];
    }

    private sealed record WidgetExecutionPatternEntry
    {
        public double HoursAgo { get; init; }
        public int ExecutionTimeMs { get; init; }
        public string TriggeredBy { get; init; } = string.Empty;
    }

    private sealed record FormSubmissionSeed
    {
        public string WidgetName { get; init; } = string.Empty;
        public string Data { get; init; } = "{}";
        public string? IpAddress { get; init; }
        public int DaysAgo { get; init; }
        public int? HoursAgo { get; init; }
    }

    private sealed record DeliveryTargetSeed
    {
        public string WidgetName { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public System.Text.Json.JsonElement? Configuration { get; init; }
        public bool IsEnabled { get; init; } = true;
    }

    private sealed record DeliveryExecutionSeed
    {
        public string DeliveryTargetName { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string? Message { get; init; }
        public string TriggeredBy { get; init; } = string.Empty;
        public int DaysAgo { get; init; }
        public int? HoursAgo { get; init; }
    }

    private sealed record WidgetConfigArchiveSeed
    {
        public string WidgetName { get; init; } = string.Empty;
        public string? Configuration { get; init; }
        public string? ChartConfig { get; init; }
        public string? HtmlTemplate { get; init; }
        public string? Note { get; init; }
        public string TriggerSource { get; init; } = "Manual";
        public string? ArchivedBy { get; init; }
        public int DaysAgo { get; init; }
    }

    private sealed record IdeaSeedData
    {
        public List<IdeaSubscriptionSeed> Subscriptions { get; init; } = [];
        public List<IdeaPostSeed> Posts { get; init; } = [];
    }

    private sealed record IdeaSubscriptionSeed
    {
        public string WidgetName { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? LabelFilter { get; init; }
        public bool IsActive { get; init; } = true;
    }

    private sealed record IdeaPostSeed
    {
        public string WidgetName { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string? Content { get; init; }
        public string? Labels { get; init; }
        public string Status { get; init; } = "Pending";
        public string? CreatedBy { get; init; }
        public int DaysAgo { get; init; }
        public int? ProcessedDaysAgo { get; init; }
        public List<IdeaResultSeed> Results { get; init; } = [];
    }

    private sealed record IdeaResultSeed
    {
        public string SubscriptionName { get; init; } = string.Empty;
        public string? ResultContent { get; init; }
        public string Status { get; init; } = "Received";
        public int DaysAgo { get; init; }
    }

    private sealed record PageSeed
    {
        public string TenantSlug { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public string? Description { get; init; }
        public List<string> WidgetNames { get; init; } = [];
        public int NarrowWidgetCount { get; init; } = 4;
    }


    /// <summary>
    /// Detects databases that were created outside of EF Core migrations (e.g., via EnsureCreated
    /// or from a previous migration set that was later squashed) and marks the corresponding
    /// migrations as applied in __EFMigrationsHistory so that MigrateAsync does not attempt
    /// to re-create tables that already exist.
    /// </summary>
    private async Task ReconcileMigrationHistoryAsync()
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        using var cmd = connection.CreateCommand();

        // Ensure the EF migrations history table exists
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            )
            """;
        await cmd.ExecuteNonQueryAsync();

        await EnsureWidgetApiActivitiesTableAsync(connection);

        // For each migration, if its characteristic schema object exists but the migration
        // is not yet recorded, mark it as applied so MigrateAsync will skip it.
        await TryMarkMigrationAppliedAsync(connection,
            "20260421161413_AddWidgetApiActivityAndInactivityFields",
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='WidgetApiActivities'");

        await TryMarkMigrationAppliedAsync(connection,
            "20260425170354_AddFormSubmissions",
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='FormSubmissions'");

        await TryMarkMigrationAppliedAsync(connection,
            "20260505164318_AddTenantAndPageSupport",
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Tenants'");

        await TryMarkMigrationAppliedAsync(connection,
            "20260507152721_AddOperationalIndexes",
            "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name='IX_WidgetExecutions_WidgetId_StartedAt'");
    }

    private static async Task EnsureWidgetApiActivitiesTableAsync(System.Data.Common.DbConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS "WidgetApiActivities" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_WidgetApiActivities" PRIMARY KEY AUTOINCREMENT,
                "WidgetId" INTEGER NOT NULL,
                "ApiEndpoint" TEXT NOT NULL,
                "UserId" TEXT NULL,
                "CalledAt" TEXT NOT NULL,
                "ResponseTimeMs" INTEGER NULL,
                "StatusCode" INTEGER NOT NULL,
                CONSTRAINT "FK_WidgetApiActivities_Widgets_WidgetId" FOREIGN KEY ("WidgetId") REFERENCES "Widgets" ("Id") ON DELETE CASCADE
            );
            """;
        await cmd.ExecuteNonQueryAsync();

        cmd.CommandText = """
            CREATE INDEX IF NOT EXISTS "IX_WidgetApiActivities_WidgetId_CalledAt"
            ON "WidgetApiActivities" ("WidgetId", "CalledAt");
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task TryMarkMigrationAppliedAsync(
        System.Data.Common.DbConnection connection,
        string migrationId,
        string schemaExistsQuery)
    {
        // Derive the EF Core product version from the assembly to keep history consistent
        var efAttr = Attribute.GetCustomAttribute(typeof(DbContext).Assembly,
            typeof(System.Reflection.AssemblyInformationalVersionAttribute))
            as System.Reflection.AssemblyInformationalVersionAttribute;
        var efVersion = efAttr?.InformationalVersion ?? "10.0.0";
        var plusIndex = efVersion.IndexOf('+');
        if (plusIndex >= 0) efVersion = efVersion[..plusIndex];

        using var cmd = connection.CreateCommand();

        // Skip if already recorded in history
        cmd.CommandText = "SELECT COUNT(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = @migrationId";
        var p = cmd.CreateParameter();
        p.ParameterName = "@migrationId";
        p.Value = migrationId;
        cmd.Parameters.Add(p);
        var alreadyRecorded = (long)(await cmd.ExecuteScalarAsync())! > 0;
        if (alreadyRecorded) return;

        // Only mark as applied if the schema already exists in the database
        cmd.CommandText = schemaExistsQuery;
        cmd.Parameters.Clear();
        var schemaExists = (long)(await cmd.ExecuteScalarAsync())! > 0;
        if (!schemaExists) return;

        cmd.CommandText = "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES (@migrationId, @productVersion)";
        var pId = cmd.CreateParameter();
        pId.ParameterName = "@migrationId";
        pId.Value = migrationId;
        cmd.Parameters.Add(pId);
        var pVer = cmd.CreateParameter();
        pVer.ParameterName = "@productVersion";
        pVer.Value = efVersion;
        cmd.Parameters.Add(pVer);
        await cmd.ExecuteNonQueryAsync();
    }
}
