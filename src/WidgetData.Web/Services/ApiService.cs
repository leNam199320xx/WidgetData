using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WidgetData.Application.DTOs;

namespace WidgetData.Web.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private readonly TokenStore _tokenStore;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ApiService(HttpClient http, TokenStore tokenStore)
    {
        _http = http;
        _tokenStore = tokenStore;
    }

    public void SetToken(string token)
    {
        _tokenStore.Token = token;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private void ApplyToken()
    {
        if (_tokenStore.Token != null)
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _tokenStore.Token);
        else
            _http.DefaultRequestHeaders.Authorization = null;
    }

    private async Task<T?> GetAsync<T>(string url)
    {
        ApplyToken();
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return default;
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    private async Task<T?> PostAsync<T>(string url, object data)
    {
        ApplyToken();
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync(url, content);
        if (!response.IsSuccessStatusCode) return default;
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseJson, _jsonOptions);
    }

    private async Task<T?> PutAsync<T>(string url, object data)
    {
        ApplyToken();
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PutAsync(url, content);
        if (!response.IsSuccessStatusCode) return default;
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseJson, _jsonOptions);
    }

    private async Task<bool> DeleteAsync(string url)
    {
        ApplyToken();
        var response = await _http.DeleteAsync(url);
        return response.IsSuccessStatusCode;
    }

    // Auth
    public Task<AuthResponseDto?> LoginAsync(LoginDto dto) => PostAsync<AuthResponseDto>("api/auth/login", dto);

    // Dashboard
    public Task<DashboardStatsDto?> GetDashboardStatsAsync() => GetAsync<DashboardStatsDto>("api/dashboard/stats");

    // Data Sources
    public Task<IEnumerable<DataSourceDto>?> GetDataSourcesAsync() => GetAsync<IEnumerable<DataSourceDto>>("api/datasources");
    public Task<DataSourceDto?> CreateDataSourceAsync(CreateDataSourceDto dto) => PostAsync<DataSourceDto>("api/datasources", dto);
    public Task<DataSourceDto?> UpdateDataSourceAsync(int id, UpdateDataSourceDto dto) => PutAsync<DataSourceDto>($"api/datasources/{id}", dto);
    public Task<bool> DeleteDataSourceAsync(int id) => DeleteAsync($"api/datasources/{id}");
    public async Task<string?> TestDataSourceAsync(int id)
    {
        ApplyToken();
        var response = await _http.PostAsync($"api/datasources/{id}/test", null);
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Dictionary<string, string>>(json, _jsonOptions);
        return result?.GetValueOrDefault("message");
    }

    // Widgets
    public Task<IEnumerable<WidgetDto>?> GetWidgetsAsync() => GetAsync<IEnumerable<WidgetDto>>("api/widgets");
    public Task<WidgetDto?> GetWidgetByIdAsync(int id) => GetAsync<WidgetDto>($"api/widgets/{id}");
    public Task<WidgetDto?> CreateWidgetAsync(CreateWidgetDto dto) => PostAsync<WidgetDto>("api/widgets", dto);
    public Task<WidgetDto?> UpdateWidgetAsync(int id, UpdateWidgetDto dto) => PutAsync<WidgetDto>($"api/widgets/{id}", dto);
    public Task<bool> DeleteWidgetAsync(int id) => DeleteAsync($"api/widgets/{id}");
    public Task<WidgetExecutionDto?> ExecuteWidgetAsync(int id) => PostAsync<WidgetExecutionDto>($"api/widgets/{id}/execute", new { });
    public Task<IEnumerable<WidgetExecutionDto>?> GetWidgetHistoryAsync(int id) => GetAsync<IEnumerable<WidgetExecutionDto>>($"api/widgets/{id}/history");
    public async Task<WidgetDto?> UpdateWidgetHtmlTemplateAsync(int id, string? htmlTemplate)
    {
        var widget = await GetWidgetByIdAsync(id);
        if (widget == null) return null;
        var dto = new UpdateWidgetDto
        {
            Name = widget.Name, FriendlyLabel = widget.FriendlyLabel, HelpText = widget.HelpText,
            WidgetType = widget.WidgetType, Description = widget.Description,
            DataSourceId = widget.DataSourceId, Configuration = widget.Configuration,
            ChartConfig = widget.ChartConfig, HtmlTemplate = htmlTemplate,
            CacheEnabled = widget.CacheEnabled, CacheTtlMinutes = widget.CacheTtlMinutes,
            IsActive = widget.IsActive, GroupIds = widget.GroupIds
        };
        return await PutAsync<WidgetDto>($"api/widgets/{id}", dto);
    }

    // Schedules
    public Task<IEnumerable<WidgetScheduleDto>?> GetSchedulesAsync() => GetAsync<IEnumerable<WidgetScheduleDto>>("api/schedules");
    public Task<WidgetScheduleDto?> CreateScheduleAsync(CreateScheduleDto dto) => PostAsync<WidgetScheduleDto>("api/schedules", dto);
    public Task<WidgetScheduleDto?> UpdateScheduleAsync(int id, UpdateScheduleDto dto) => PutAsync<WidgetScheduleDto>($"api/schedules/{id}", dto);
    public Task<bool> DeleteScheduleAsync(int id) => DeleteAsync($"api/schedules/{id}");
    public async Task<bool> EnableScheduleAsync(int id)
    {
        ApplyToken();
        var response = await _http.PostAsync($"api/schedules/{id}/enable", null);
        return response.IsSuccessStatusCode;
    }
    public async Task<bool> DisableScheduleAsync(int id)
    {
        ApplyToken();
        var response = await _http.PostAsync($"api/schedules/{id}/disable", null);
        return response.IsSuccessStatusCode;
    }

    // Users
    public Task<IEnumerable<UserDto>?> GetUsersAsync() => GetAsync<IEnumerable<UserDto>>("api/users");
    public Task<object?> CreateUserAsync(RegisterDto dto) => PostAsync<object>("api/users", dto);

    // Widget Groups
    public Task<IEnumerable<WidgetGroupDto>?> GetWidgetGroupsAsync() => GetAsync<IEnumerable<WidgetGroupDto>>("api/widget-groups");
    public Task<WidgetGroupDto?> GetWidgetGroupAsync(int id) => GetAsync<WidgetGroupDto>($"api/widget-groups/{id}");
    public Task<WidgetGroupDto?> CreateWidgetGroupAsync(CreateWidgetGroupDto dto) => PostAsync<WidgetGroupDto>("api/widget-groups", dto);
    public Task<WidgetGroupDto?> UpdateWidgetGroupAsync(int id, UpdateWidgetGroupDto dto) => PutAsync<WidgetGroupDto>($"api/widget-groups/{id}", dto);
    public Task<bool> DeleteWidgetGroupAsync(int id) => DeleteAsync($"api/widget-groups/{id}");

    // Permissions
    public Task<IEnumerable<UserPermissionDto>?> GetUserPermissionsAsync(string userId) => GetAsync<IEnumerable<UserPermissionDto>>($"api/permissions/user/{userId}");
    public Task<IEnumerable<UserPermissionDto>?> GetWidgetPermissionsAsync(int widgetId) => GetAsync<IEnumerable<UserPermissionDto>>($"api/permissions/widget/{widgetId}");
    public Task<IEnumerable<UserPermissionDto>?> GetGroupPermissionsAsync(int groupId) => GetAsync<IEnumerable<UserPermissionDto>>($"api/permissions/group/{groupId}");
    public Task<UserPermissionDto?> AssignWidgetPermissionAsync(AssignWidgetPermissionDto dto) => PostAsync<UserPermissionDto>("api/permissions/widget", dto);
    public Task<UserPermissionDto?> AssignGroupPermissionAsync(AssignGroupPermissionDto dto) => PostAsync<UserPermissionDto>("api/permissions/group", dto);
    public Task<bool> RemoveWidgetPermissionAsync(int permissionId) => DeleteAsync($"api/permissions/widget/{permissionId}");
    public Task<bool> RemoveGroupPermissionAsync(int permissionId) => DeleteAsync($"api/permissions/group/{permissionId}");

    // Delivery Targets
    public Task<IEnumerable<DeliveryTargetDto>?> GetDeliveryTargetsAsync(int widgetId) => GetAsync<IEnumerable<DeliveryTargetDto>>($"api/delivery-targets/widget/{widgetId}");
    public Task<DeliveryTargetDto?> CreateDeliveryTargetAsync(CreateDeliveryTargetDto dto) => PostAsync<DeliveryTargetDto>("api/delivery-targets", dto);
    public Task<DeliveryTargetDto?> UpdateDeliveryTargetAsync(int id, UpdateDeliveryTargetDto dto) => PutAsync<DeliveryTargetDto>($"api/delivery-targets/{id}", dto);
    public Task<bool> DeleteDeliveryTargetAsync(int id) => DeleteAsync($"api/delivery-targets/{id}");
    public Task<DeliveryExecutionDto?> TriggerDeliveryAsync(int widgetId, int deliveryTargetId) => PostAsync<DeliveryExecutionDto>($"api/widgets/{widgetId}/deliver/{deliveryTargetId}", new { });
    public Task<IEnumerable<DeliveryExecutionDto>?> GetDeliveryExecutionsAsync(int widgetId) => GetAsync<IEnumerable<DeliveryExecutionDto>>($"api/widgets/{widgetId}/deliveries");

    public async Task<bool> TriggerScheduleAsync(int id)
    {
        ApplyToken();
        var response = await _http.PostAsync($"api/schedules/{id}/trigger", null);
        return response.IsSuccessStatusCode;
    }

    // Config Archives (nested under widgets - used by WidgetConfigure page)
    public Task<IEnumerable<WidgetConfigArchiveDto>?> GetWidgetConfigArchivesAsync(int widgetId)
        => GetAsync<IEnumerable<WidgetConfigArchiveDto>>($"api/widgets/{widgetId}/config-archives");
    public Task<WidgetConfigArchiveDto?> CreateWidgetConfigArchiveAsync(int widgetId, CreateWidgetConfigArchiveDto dto)
        => PostAsync<WidgetConfigArchiveDto>($"api/widgets/{widgetId}/config-archives", dto);
    public Task<WidgetDto?> RestoreWidgetConfigArchiveAsync(int widgetId, int archiveId)
        => PostAsync<WidgetDto>($"api/widgets/{widgetId}/config-archives/{archiveId}/restore", new { });
    public Task<bool> DeleteWidgetConfigArchiveAsync(int widgetId, int archiveId)
        => DeleteAsync($"api/widgets/{widgetId}/config-archives/{archiveId}");

    // Config Archives (flat - used by /config-archives admin page)
    public Task<IEnumerable<WidgetConfigArchiveDto>?> GetAllConfigArchivesAsync()
        => GetAsync<IEnumerable<WidgetConfigArchiveDto>>("api/widget-config-archives");

    // Export
    public string GetExportUrl(int widgetId, string format) => $"api/widgets/{widgetId}/export?format={format}";
    public async Task<byte[]?> ExportWidgetAsync(int widgetId, string format)
    {
        ApplyToken();
        var response = await _http.GetAsync($"api/widgets/{widgetId}/export?format={format}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadAsByteArrayAsync();
    }

    // Idea Board
    public Task<IdeaPostDto?> CreateIdeaPostAsync(CreateIdeaPostDto dto) => PostAsync<IdeaPostDto>("api/idea-board/posts", dto);
    public Task<IdeaPostDto?> GetIdeaPostAsync(int id) => GetAsync<IdeaPostDto>($"api/idea-board/posts/{id}");
    public Task<IEnumerable<IdeaPostDto>?> GetIdeaPostsByWidgetAsync(int widgetId) => GetAsync<IEnumerable<IdeaPostDto>>($"api/idea-board/widgets/{widgetId}/posts");
    public Task<IEnumerable<IdeaResultDto>?> GetIdeaResultsAsync(int postId) => GetAsync<IEnumerable<IdeaResultDto>>($"api/idea-board/posts/{postId}/results");
    public Task<IEnumerable<IdeaSubscriptionDto>?> GetIdeaSubscriptionsAsync(int widgetId) => GetAsync<IEnumerable<IdeaSubscriptionDto>>($"api/idea-board/widgets/{widgetId}/subscriptions");
    public Task<IdeaSubscriptionDto?> CreateIdeaSubscriptionAsync(CreateIdeaSubscriptionDto dto) => PostAsync<IdeaSubscriptionDto>("api/idea-board/subscriptions", dto);
    public Task<IdeaSubscriptionDto?> UpdateIdeaSubscriptionAsync(int id, UpdateIdeaSubscriptionDto dto) => PutAsync<IdeaSubscriptionDto>($"api/idea-board/subscriptions/{id}", dto);
    public Task<bool> DeleteIdeaSubscriptionAsync(int id) => DeleteAsync($"api/idea-board/subscriptions/{id}");

    // Reports
    public Task<IEnumerable<WidgetGroupDto>?> GetReportPagesAsync() => GetAsync<IEnumerable<WidgetGroupDto>>("api/reports/pages");
    public Task<ReportPageDto?> GetReportPageAsync(int id) => GetAsync<ReportPageDto>($"api/reports/pages/{id}");
    public Task<WidgetDataDto?> GetWidgetDataAsync(int widgetId) => GetAsync<WidgetDataDto>($"api/reports/widgets/{widgetId}/data");

    // Dashboard Pages (alias for Widget Groups with builder support)
    public Task<IEnumerable<WidgetGroupDto>?> GetDashboardPagesAsync() => GetAsync<IEnumerable<WidgetGroupDto>>("api/widget-groups");
    public Task<WidgetGroupDto?> GetDashboardPageAsync(int id) => GetAsync<WidgetGroupDto>($"api/widget-groups/{id}");
    public Task<WidgetGroupDto?> CreateDashboardPageAsync(CreateWidgetGroupDto dto) => PostAsync<WidgetGroupDto>("api/widget-groups", dto);
    public Task<WidgetGroupDto?> UpdateDashboardPageAsync(int id, UpdateWidgetGroupDto dto) => PutAsync<WidgetGroupDto>($"api/widget-groups/{id}", dto);
    public Task<bool> DeleteDashboardPageAsync(int id) => DeleteAsync($"api/widget-groups/{id}");

    // Form Widget
    public Task<FormSchemaDto?> GetFormSchemaAsync(int widgetId) => GetAsync<FormSchemaDto>($"api/form/{widgetId}/schema");
    public Task<IEnumerable<FormSubmissionDto>?> GetFormSubmissionsAsync(int widgetId) => GetAsync<IEnumerable<FormSubmissionDto>>($"api/form/{widgetId}/submissions");
    public Task<bool> DeleteFormSubmissionAsync(int id) => DeleteAsync($"api/form/submissions/{id}");

    // Tenants (SuperAdmin)
    public Task<IEnumerable<TenantDto>?> GetTenantsAsync() => GetAsync<IEnumerable<TenantDto>>("api/tenants");
    public Task<TenantDto?> CreateTenantAsync(CreateTenantDto dto) => PostAsync<TenantDto>("api/tenants", dto);
    public Task<TenantDto?> UpdateTenantAsync(int id, UpdateTenantDto dto) => PutAsync<TenantDto>($"api/tenants/{id}", dto);
    public Task<bool> DeleteTenantAsync(int id) => DeleteAsync($"api/tenants/{id}");
    public Task<AdminStatsDto?> GetAdminStatsAsync() => GetAsync<AdminStatsDto>("api/tenants/admin-stats");

    // Pages (Site pages)
    public Task<IEnumerable<PageDto>?> GetPagesAsync() => GetAsync<IEnumerable<PageDto>>("api/pages");
    public Task<PageDto?> GetPageByIdAsync(int id) => GetAsync<PageDto>($"api/pages/{id}");
    public Task<PageDto?> CreatePageAsync(CreatePageDto dto) => PostAsync<PageDto>("api/pages", dto);
    public Task<PageDto?> UpdatePageAsync(int id, UpdatePageDto dto) => PutAsync<PageDto>($"api/pages/{id}", dto);
    public Task<bool> DeletePageAsync(int id) => DeleteAsync($"api/pages/{id}");
    public Task AddWidgetToPageAsync(int pageId, int widgetId, int position, int width)
        => PostAsync<object>($"api/pages/{pageId}/widgets", new PageWidgetLayoutDto { WidgetId = widgetId, Position = position, Width = width });
    public Task RemoveWidgetFromPageAsync(int pageId, int widgetId) => DeleteAsync($"api/pages/{pageId}/widgets/{widgetId}");
}
