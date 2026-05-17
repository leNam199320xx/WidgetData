using WidgetData.Domain.Entities;

namespace WidgetData.Infrastructure.Data.Json.Repositories;

/// <summary>
/// Widget JSON Repository
/// </summary>
public class JsonWidgetRepository : BaseJsonRepository<Widget>, IJsonWidgetRepository
{
    public JsonWidgetRepository(JsonDataProvider jsonProvider)
        : base(jsonProvider, "widgets")
    {
    }

    protected override int GetEntityId(Widget entity) => entity.Id;

    public async Task<List<Widget>> GetActiveWidgetsAsync()
    {
        var all = await GetAllAsync();
        return all.Where(w => w.IsActive).ToList();
    }

    public async Task<List<Widget>> GetByDataSourceAsync(int dataSourceId)
    {
        var all = await GetAllAsync();
        return all.Where(w => w.DataSourceId == dataSourceId).ToList();
    }

    public async Task<List<Widget>> GetByGroupAsync(int groupId)
    {
        var all = await GetAllAsync();
        return all.Where(w => w.Id == groupId).ToList();
    }

    public override async Task<List<Widget>> GetByTenantAsync(int? tenantId)
    {
        var all = await GetAllAsync();
        if (tenantId == null) return all;
        return all.Where(w => w.TenantId == tenantId || w.TenantId == null).ToList();
    }
}

/// <summary>
/// DataSource JSON Repository
/// </summary>
public class JsonDataSourceRepository : BaseJsonRepository<DataSource>, IJsonDataSourceRepository
{
    public JsonDataSourceRepository(JsonDataProvider jsonProvider)
        : base(jsonProvider, "datasources")
    {
    }

    protected override int GetEntityId(DataSource entity) => entity.Id;

    public async Task<List<DataSource>> GetActiveDataSourcesAsync()
    {
        var all = await GetAllAsync();
        return all.Where(d => d.IsActive).ToList();
    }

    public async Task<DataSource?> GetByNameAsync(string name)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(d => d.Name == name);
    }

    public override async Task<List<DataSource>> GetByTenantAsync(int? tenantId)
    {
        var all = await GetAllAsync();
        if (tenantId == null) return all;
        return all.Where(d => d.TenantId == tenantId || d.TenantId == null).ToList();
    }
}

/// <summary>
/// WidgetSchedule JSON Repository
/// </summary>
public class JsonScheduleRepository : BaseJsonRepository<WidgetSchedule>, IJsonScheduleRepository
{
    public JsonScheduleRepository(JsonDataProvider jsonProvider)
        : base(jsonProvider, "schedules")
    {
    }

    protected override int GetEntityId(WidgetSchedule entity) => entity.Id;
    protected override string GetFileName(int id) => $"schedule-{id}.json";

    public async Task<List<WidgetSchedule>> GetActiveSchedulesAsync()
    {
        var all = await GetAllAsync();
        return all.Where(s => s.IsEnabled).ToList();
    }

    public async Task<List<WidgetSchedule>> GetByWidgetAsync(int widgetId)
    {
        var all = await GetAllAsync();
        return all.Where(s => s.WidgetId == widgetId).ToList();
    }
}

/// <summary>
/// WidgetExecution JSON Repository
/// </summary>
public class JsonExecutionRepository : BaseJsonRepository<WidgetExecution>, IJsonExecutionRepository
{
    public JsonExecutionRepository(JsonDataProvider jsonProvider)
        : base(jsonProvider, "executions")
    {
    }

    protected override int GetEntityId(WidgetExecution entity) => entity.Id;

    public async Task<List<WidgetExecution>> GetByWidgetAsync(int widgetId, int limit = 100)
    {
        var all = await GetAllAsync();
        return all.Where(e => e.WidgetId == widgetId)
                  .OrderByDescending(e => e.StartedAt)
                  .Take(limit)
                  .ToList();
    }

    public async Task<List<WidgetExecution>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var all = await GetAllAsync();
        return all.Where(e => e.StartedAt >= from && e.StartedAt <= to)
                  .OrderByDescending(e => e.StartedAt)
                  .ToList();
    }
}

/// <summary>
/// Page JSON Repository
/// </summary>
public class JsonPageRepository : BaseJsonRepository<Page>, IJsonPageRepository
{
    public JsonPageRepository(JsonDataProvider jsonProvider)
        : base(jsonProvider, "pages")
    {
    }

    protected override int GetEntityId(Page entity) => entity.Id;

    public async Task<Page?> GetBySlugAsync(string slug, int? tenantId)
    {
        var all = await GetAllAsync();
        return tenantId.HasValue
            ? all.FirstOrDefault(p => p.Slug == slug && (p.TenantId == tenantId.Value || p.TenantId == 0))
            : all.FirstOrDefault(p => p.Slug == slug);
    }

    public async Task<List<Page>> GetPublishedAsync()
    {
        var all = await GetAllAsync();
        return all.Where(p => p.PublishedAt.HasValue).ToList();
    }

    public override async Task<List<Page>> GetByTenantAsync(int? tenantId)
    {
        var all = await GetAllAsync();
        if (tenantId == null) return all;
        return all.Where(p => p.TenantId == tenantId.Value || p.TenantId == 0).ToList();
    }
}

/// <summary>
/// PageVersion JSON Repository
/// </summary>
public class JsonPageVersionRepository : BaseJsonRepository<PageVersion>, IJsonPageVersionRepository
{
    public JsonPageVersionRepository(JsonDataProvider jsonProvider)
        : base(jsonProvider, "page-versions")
    {
    }

    protected override int GetEntityId(PageVersion entity) => entity.Id;
    protected override string GetFileName(int id) => $"version-{id}.json";

    public async Task<List<PageVersion>> GetByPageAsync(int pageId)
    {
        var all = await GetAllAsync();
        return all.Where(v => v.PageId == pageId)
                  .OrderByDescending(v => v.VersionNumber)
                  .ToList();
    }

    public async Task<PageVersion?> GetLatestByPageAsync(int pageId)
    {
        var versions = await GetByPageAsync(pageId);
        return versions.FirstOrDefault();
    }
}

/// <summary>
/// PageWidget JSON Repository
/// </summary>
public class JsonPageWidgetRepository : BaseJsonRepository<PageWidget>, IJsonPageWidgetRepository
{
    public JsonPageWidgetRepository(JsonDataProvider jsonProvider)
        : base(jsonProvider, "page-widgets")
    {
    }

    protected override int GetEntityId(PageWidget entity) => entity.Id;
    protected override string GetFileName(int id) => $"widget-{id}.json";

    public async Task<List<PageWidget>> GetByPageAsync(int pageId)
    {
        var all = await GetAllAsync();
        return all.Where(pw => pw.PageId == pageId).ToList();
    }

    public async Task<List<PageWidget>> GetByWidgetAsync(int widgetId)
    {
        var all = await GetAllAsync();
        return all.Where(pw => pw.WidgetId == widgetId).ToList();
    }
}

/// <summary>
/// WidgetGroup JSON Repository
/// </summary>
public class JsonWidgetGroupRepository : BaseJsonRepository<WidgetGroup>, IJsonWidgetGroupRepository
{
    public JsonWidgetGroupRepository(JsonDataProvider jsonProvider)
        : base(jsonProvider, "groups")
    {
    }

    protected override int GetEntityId(WidgetGroup entity) => entity.Id;

    public async Task<List<WidgetGroup>> GetActiveGroupsAsync()
    {
        var all = await GetAllAsync();
        return all.Where(g => g.IsActive).ToList();
    }

    public override async Task<List<WidgetGroup>> GetByTenantAsync(int? tenantId)
    {
        var all = await GetAllAsync();
        if (tenantId == null) return all;
        return all.Where(g => g.TenantId == tenantId || g.TenantId == null).ToList();
    }
}

/// <summary>
/// WidgetGroupMember JSON Repository
/// </summary>
public class JsonWidgetGroupMemberRepository : BaseJsonRepository<WidgetGroupMember>, IJsonWidgetGroupMemberRepository
{
    public JsonWidgetGroupMemberRepository(JsonDataProvider jsonProvider)
        : base(jsonProvider, "group-members")
    {
    }

    protected override int GetEntityId(WidgetGroupMember entity) => ToCompositeId(entity.WidgetGroupId, entity.WidgetId);

    public async Task<List<WidgetGroupMember>> GetByGroupAsync(int groupId)
    {
        var all = await GetAllAsync();
        return all.Where(m => m.WidgetGroupId == groupId).ToList();
    }

    public async Task<List<WidgetGroupMember>> GetByWidgetAsync(int widgetId)
    {
        var all = await GetAllAsync();
        return all.Where(m => m.WidgetId == widgetId).ToList();
    }

    private static int ToCompositeId(int groupId, int widgetId)
        => unchecked((groupId * 1_000_000) + widgetId);
}

/// <summary>
/// WidgetConfigArchive JSON Repository
/// </summary>
public class JsonWidgetConfigArchiveRepository : BaseJsonRepository<WidgetConfigArchive>, IJsonWidgetConfigArchiveRepository
{
    public JsonWidgetConfigArchiveRepository(JsonDataProvider jsonProvider)
        : base(jsonProvider, "widget-archives")
    {
    }

    protected override int GetEntityId(WidgetConfigArchive entity) => entity.Id;
    protected override string GetFileName(int id) => $"archive-{id}.json";

    public async Task<List<WidgetConfigArchive>> GetByWidgetAsync(int widgetId)
    {
        var all = await GetAllAsync();
        return all.Where(a => a.WidgetId == widgetId)
                  .OrderByDescending(a => a.ArchivedAt)
                  .ToList();
    }

    public async Task<WidgetConfigArchive?> GetLatestByWidgetAsync(int widgetId)
    {
        var archives = await GetByWidgetAsync(widgetId);
        return archives.FirstOrDefault();
    }
}

/// <summary>
/// DeliveryTarget JSON Repository
/// </summary>
public class JsonDeliveryTargetRepository : BaseJsonRepository<DeliveryTarget>, IJsonDeliveryTargetRepository
{
    public JsonDeliveryTargetRepository(JsonDataProvider jsonProvider)
        : base(jsonProvider, "delivery")
    {
    }

    protected override int GetEntityId(DeliveryTarget entity) => entity.Id;
    protected override string GetFileName(int id) => $"target-{id}.json";

    public async Task<List<DeliveryTarget>> GetByWidgetAsync(int widgetId)
    {
        var all = await GetAllAsync();
        return all.Where(t => t.WidgetId == widgetId).ToList();
    }

    public async Task<List<DeliveryTarget>> GetActiveTargetsAsync()
    {
        var all = await GetAllAsync();
        return all.Where(t => t.IsEnabled).ToList();
    }
}

/// <summary>
/// DeliveryExecution JSON Repository
/// </summary>
public class JsonDeliveryExecutionRepository : BaseJsonRepository<DeliveryExecution>, IJsonDeliveryExecutionRepository
{
    public JsonDeliveryExecutionRepository(JsonDataProvider jsonProvider)
        : base(jsonProvider, "delivery")
    {
    }

    protected override int GetEntityId(DeliveryExecution entity) => entity.Id;
    protected override string GetFileName(int id) => $"exec-{id}.json";

    public async Task<List<DeliveryExecution>> GetByTargetAsync(int targetId)
    {
        var all = await GetAllAsync();
        return all.Where(e => e.DeliveryTargetId == targetId)
                  .OrderByDescending(e => e.ExecutedAt)
                  .ToList();
    }

    public async Task<List<DeliveryExecution>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var all = await GetAllAsync();
        return all.Where(e => e.ExecutedAt >= from && e.ExecutedAt <= to)
                  .OrderByDescending(e => e.ExecutedAt)
                  .ToList();
    }
}

/// <summary>
/// IdeaPost JSON Repository
/// </summary>
public class JsonIdeaPostRepository : BaseJsonRepository<IdeaPost>, IJsonIdeaPostRepository
{
    public JsonIdeaPostRepository(JsonDataProvider jsonProvider)
        : base(jsonProvider, "ideas")
    {
    }

    protected override int GetEntityId(IdeaPost entity) => entity.Id;

    public async Task<List<IdeaPost>> GetByWidgetAsync(int widgetId)
    {
        var all = await GetAllAsync();
        return all.Where(p => p.WidgetId == widgetId)
                  .OrderByDescending(p => p.CreatedAt)
                  .ToList();
    }

    public async Task<List<IdeaPost>> GetApprovedAsync()
    {
        var all = await GetAllAsync();
        return all.Where(p => p.Status == "Approved")
                  .OrderByDescending(p => p.CreatedAt)
                  .ToList();
    }
}

/// <summary>
/// IdeaSubscription JSON Repository
/// </summary>
public class JsonIdeaSubscriptionRepository : BaseJsonRepository<IdeaSubscription>, IJsonIdeaSubscriptionRepository
{
    public JsonIdeaSubscriptionRepository(JsonDataProvider jsonProvider)
        : base(jsonProvider, "ideas")
    {
    }

    protected override int GetEntityId(IdeaSubscription entity) => entity.Id;
    protected override string GetFileName(int id) => $"subscription-{id}.json";

    public async Task<List<IdeaSubscription>> GetByWidgetAsync(int widgetId)
    {
        var all = await GetAllAsync();
        return all.Where(s => s.WidgetId == widgetId).ToList();
    }

    public async Task<List<IdeaSubscription>> GetByUserAsync(string userId)
    {
        var all = await GetAllAsync();
        return all.Where(s => s.CreatedBy == userId).ToList();
    }
}

/// <summary>
/// IdeaResult JSON Repository
/// </summary>
public class JsonIdeaResultRepository : BaseJsonRepository<IdeaResult>, IJsonIdeaResultRepository
{
    public JsonIdeaResultRepository(JsonDataProvider jsonProvider)
        : base(jsonProvider, "ideas")
    {
    }

    protected override int GetEntityId(IdeaResult entity) => entity.Id;
    protected override string GetFileName(int id) => $"result-{id}.json";

    public async Task<List<IdeaResult>> GetByPostAsync(int postId)
    {
        var all = await GetAllAsync();
        return all.Where(r => r.IdeaPostId == postId).ToList();
    }

    public async Task<List<IdeaResult>> GetBySubscriptionAsync(int subscriptionId)
    {
        var all = await GetAllAsync();
        return all.Where(r => r.IdeaSubscriptionId == subscriptionId).ToList();
    }
}

/// <summary>
/// FormSubmission JSON Repository
/// </summary>
public class JsonFormSubmissionRepository : BaseJsonRepository<FormSubmission>, IJsonFormSubmissionRepository
{
    public JsonFormSubmissionRepository(JsonDataProvider jsonProvider)
        : base(jsonProvider, "forms")
    {
    }

    protected override int GetEntityId(FormSubmission entity) => entity.Id;

    public async Task<List<FormSubmission>> GetByWidgetAsync(int widgetId)
    {
        var all = await GetAllAsync();
        return all.Where(f => f.WidgetId == widgetId)
                  .OrderByDescending(f => f.SubmittedAt)
                  .ToList();
    }

    public async Task<List<FormSubmission>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var all = await GetAllAsync();
        return all.Where(f => f.SubmittedAt >= from && f.SubmittedAt <= to)
                  .OrderByDescending(f => f.SubmittedAt)
                  .ToList();
    }

    public override async Task<List<FormSubmission>> GetByTenantAsync(int? tenantId)
    {
        var all = await GetAllAsync();
        return all.Where(f => f.TenantId == tenantId || f.TenantId == null).ToList();
    }
}

/// <summary>
/// WidgetApiActivity JSON Repository
/// </summary>
public class JsonWidgetActivityRepository : BaseJsonRepository<WidgetApiActivity>, IJsonWidgetActivityRepository
{
    public JsonWidgetActivityRepository(JsonDataProvider jsonProvider)
        : base(jsonProvider, "executions")
    {
    }

    protected override int GetEntityId(WidgetApiActivity entity) => entity.Id;
    protected override string GetFileName(int id) => $"activity-{id}.json";

    public async Task<List<WidgetApiActivity>> GetByWidgetAsync(int widgetId, int limit = 100)
    {
        var all = await GetAllAsync();
        return all.Where(a => a.WidgetId == widgetId)
                  .OrderByDescending(a => a.CalledAt)
                  .Take(limit)
                  .ToList();
    }

    public async Task<List<WidgetApiActivity>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var all = await GetAllAsync();
        return all.Where(a => a.CalledAt >= from && a.CalledAt <= to)
                  .OrderByDescending(a => a.CalledAt)
                  .ToList();
    }
}
