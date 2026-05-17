using WidgetData.Domain.Entities;

namespace WidgetData.Infrastructure.Data.Json.Repositories;

/// <summary>
/// Base interface for JSON-based repositories
/// </summary>
public interface IJsonRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<List<T>> GetAllAsync();
    Task<List<T>> GetByTenantAsync(int? tenantId);
    Task<T> CreateAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}

/// <summary>
/// Widget Repository - JSON-based
/// </summary>
public interface IJsonWidgetRepository : IJsonRepository<Widget>
{
    Task<List<Widget>> GetActiveWidgetsAsync();
    Task<List<Widget>> GetByDataSourceAsync(int dataSourceId);
    Task<List<Widget>> GetByGroupAsync(int groupId);
}

/// <summary>
/// DataSource Repository - JSON-based
/// </summary>
public interface IJsonDataSourceRepository : IJsonRepository<DataSource>
{
    Task<List<DataSource>> GetActiveDataSourcesAsync();
    Task<DataSource?> GetByNameAsync(string name);
}

/// <summary>
/// WidgetSchedule Repository - JSON-based
/// </summary>
public interface IJsonScheduleRepository : IJsonRepository<WidgetSchedule>
{
    Task<List<WidgetSchedule>> GetActiveSchedulesAsync();
    Task<List<WidgetSchedule>> GetByWidgetAsync(int widgetId);
}

/// <summary>
/// WidgetExecution Repository - JSON-based
/// </summary>
public interface IJsonExecutionRepository : IJsonRepository<WidgetExecution>
{
    Task<List<WidgetExecution>> GetByWidgetAsync(int widgetId, int limit = 100);
    Task<List<WidgetExecution>> GetByDateRangeAsync(DateTime from, DateTime to);
}

/// <summary>
/// Page Repository - JSON-based
/// </summary>
public interface IJsonPageRepository : IJsonRepository<Page>
{
    Task<Page?> GetBySlugAsync(string slug, int? tenantId);
    Task<List<Page>> GetPublishedAsync();
}

/// <summary>
/// PageVersion Repository - JSON-based
/// </summary>
public interface IJsonPageVersionRepository : IJsonRepository<PageVersion>
{
    Task<List<PageVersion>> GetByPageAsync(int pageId);
    Task<PageVersion?> GetLatestByPageAsync(int pageId);
}

/// <summary>
/// PageWidget Repository - JSON-based
/// </summary>
public interface IJsonPageWidgetRepository : IJsonRepository<PageWidget>
{
    Task<List<PageWidget>> GetByPageAsync(int pageId);
    Task<List<PageWidget>> GetByWidgetAsync(int widgetId);
}

/// <summary>
/// WidgetGroup Repository - JSON-based
/// </summary>
public interface IJsonWidgetGroupRepository : IJsonRepository<WidgetGroup>
{
    Task<List<WidgetGroup>> GetActiveGroupsAsync();
}

/// <summary>
/// WidgetGroupMember Repository - JSON-based
/// </summary>
public interface IJsonWidgetGroupMemberRepository : IJsonRepository<WidgetGroupMember>
{
    Task<List<WidgetGroupMember>> GetByGroupAsync(int groupId);
    Task<List<WidgetGroupMember>> GetByWidgetAsync(int widgetId);
}

/// <summary>
/// WidgetConfigArchive Repository - JSON-based
/// </summary>
public interface IJsonWidgetConfigArchiveRepository : IJsonRepository<WidgetConfigArchive>
{
    Task<List<WidgetConfigArchive>> GetByWidgetAsync(int widgetId);
    Task<WidgetConfigArchive?> GetLatestByWidgetAsync(int widgetId);
}

/// <summary>
/// DeliveryTarget Repository - JSON-based
/// </summary>
public interface IJsonDeliveryTargetRepository : IJsonRepository<DeliveryTarget>
{
    Task<List<DeliveryTarget>> GetByWidgetAsync(int widgetId);
    Task<List<DeliveryTarget>> GetActiveTargetsAsync();
}

/// <summary>
/// DeliveryExecution Repository - JSON-based
/// </summary>
public interface IJsonDeliveryExecutionRepository : IJsonRepository<DeliveryExecution>
{
    Task<List<DeliveryExecution>> GetByTargetAsync(int targetId);
    Task<List<DeliveryExecution>> GetByDateRangeAsync(DateTime from, DateTime to);
}

/// <summary>
/// IdeaPost Repository - JSON-based
/// </summary>
public interface IJsonIdeaPostRepository : IJsonRepository<IdeaPost>
{
    Task<List<IdeaPost>> GetByWidgetAsync(int widgetId);
    Task<List<IdeaPost>> GetApprovedAsync();
}

/// <summary>
/// IdeaSubscription Repository - JSON-based
/// </summary>
public interface IJsonIdeaSubscriptionRepository : IJsonRepository<IdeaSubscription>
{
    Task<List<IdeaSubscription>> GetByWidgetAsync(int widgetId);
    Task<List<IdeaSubscription>> GetByUserAsync(string userId);
}

/// <summary>
/// IdeaResult Repository - JSON-based
/// </summary>
public interface IJsonIdeaResultRepository : IJsonRepository<IdeaResult>
{
    Task<List<IdeaResult>> GetByPostAsync(int postId);
    Task<List<IdeaResult>> GetBySubscriptionAsync(int subscriptionId);
}

/// <summary>
/// FormSubmission Repository - JSON-based
/// </summary>
public interface IJsonFormSubmissionRepository : IJsonRepository<FormSubmission>
{
    Task<List<FormSubmission>> GetByWidgetAsync(int widgetId);
    Task<List<FormSubmission>> GetByDateRangeAsync(DateTime from, DateTime to);
}

/// <summary>
/// WidgetApiActivity Repository - JSON-based
/// </summary>
public interface IJsonWidgetActivityRepository : IJsonRepository<WidgetApiActivity>
{
    Task<List<WidgetApiActivity>> GetByWidgetAsync(int widgetId, int limit = 100);
    Task<List<WidgetApiActivity>> GetByDateRangeAsync(DateTime from, DateTime to);
}
