using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;

namespace WidgetData.Application.Interfaces;

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

public interface IJsonWidgetRepository : IJsonRepository<Widget>
{
    Task<List<Widget>> GetActiveWidgetsAsync();
    Task<List<Widget>> GetByDataSourceAsync(int dataSourceId);
    Task<List<Widget>> GetByGroupAsync(int groupId);
}

public interface IJsonDataSourceRepository : IJsonRepository<DataSource>
{
    Task<List<DataSource>> GetActiveDataSourcesAsync();
    Task<DataSource?> GetByNameAsync(string name);
}

public interface IJsonScheduleRepository : IJsonRepository<WidgetSchedule>
{
    Task<int> CountAsync();
    Task<int> CountEnabledAsync();
    Task<List<WidgetSchedule>> GetActiveSchedulesAsync();
    Task<List<WidgetSchedule>> GetByWidgetAsync(int widgetId);
}

public interface IJsonExecutionRepository : IJsonRepository<WidgetExecution>
{
    Task<int> CountAsync();
    Task<int> CountByStatusAsync(ExecutionStatus status);
    Task<List<WidgetExecution>> GetByWidgetAsync(int widgetId, int limit = 100);
    Task<List<WidgetExecution>> GetByDateRangeAsync(DateTime from, DateTime to);
}

public interface IJsonPageRepository : IJsonRepository<Page>
{
    Task<Page?> GetBySlugAsync(string slug, int? tenantId);
    Task<List<Page>> GetPublishedAsync();
}

public interface IJsonPageVersionRepository : IJsonRepository<PageVersion>
{
    Task<List<PageVersion>> GetByPageAsync(int pageId);
    Task<PageVersion?> GetLatestByPageAsync(int pageId);
}

public interface IJsonPageWidgetRepository : IJsonRepository<PageWidget>
{
    Task<List<PageWidget>> GetByPageAsync(int pageId);
    Task<List<PageWidget>> GetByWidgetAsync(int widgetId);
}

public interface IJsonWidgetGroupRepository : IJsonRepository<WidgetGroup>
{
    Task<List<WidgetGroup>> GetActiveGroupsAsync();
}

public interface IJsonWidgetGroupMemberRepository : IJsonRepository<WidgetGroupMember>
{
    Task<List<WidgetGroupMember>> GetByGroupAsync(int groupId);
    Task<List<WidgetGroupMember>> GetByWidgetAsync(int widgetId);
}

public interface IJsonWidgetConfigArchiveRepository : IJsonRepository<WidgetConfigArchive>
{
    Task<List<WidgetConfigArchive>> GetByWidgetAsync(int widgetId);
    Task<WidgetConfigArchive?> GetLatestByWidgetAsync(int widgetId);
}

public interface IJsonDeliveryTargetRepository : IJsonRepository<DeliveryTarget>
{
    Task<List<DeliveryTarget>> GetByWidgetAsync(int widgetId);
    Task<List<DeliveryTarget>> GetActiveTargetsAsync();
}

public interface IJsonDeliveryExecutionRepository : IJsonRepository<DeliveryExecution>
{
    Task<List<DeliveryExecution>> GetByTargetAsync(int targetId);
    Task<List<DeliveryExecution>> GetByDateRangeAsync(DateTime from, DateTime to);
}

public interface IJsonIdeaPostRepository : IJsonRepository<IdeaPost>
{
    Task<List<IdeaPost>> GetByWidgetAsync(int widgetId);
    Task<List<IdeaPost>> GetApprovedAsync();
}

public interface IJsonIdeaSubscriptionRepository : IJsonRepository<IdeaSubscription>
{
    Task<List<IdeaSubscription>> GetByWidgetAsync(int widgetId);
    Task<List<IdeaSubscription>> GetByUserAsync(string userId);
}

public interface IJsonIdeaResultRepository : IJsonRepository<IdeaResult>
{
    Task<List<IdeaResult>> GetByPostAsync(int postId);
    Task<List<IdeaResult>> GetBySubscriptionAsync(int subscriptionId);
}

public interface IJsonFormSubmissionRepository : IJsonRepository<FormSubmission>
{
    Task<List<FormSubmission>> GetByWidgetAsync(int widgetId);
    Task<List<FormSubmission>> GetByDateRangeAsync(DateTime from, DateTime to);
}

public interface IJsonWidgetActivityRepository : IJsonRepository<WidgetApiActivity>
{
    Task<List<WidgetApiActivity>> GetByWidgetAsync(int widgetId, int limit = 100);
    Task<List<WidgetApiActivity>> GetByDateRangeAsync(DateTime from, DateTime to);
}
