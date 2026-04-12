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
    public Task<WidgetDto?> CreateWidgetAsync(CreateWidgetDto dto) => PostAsync<WidgetDto>("api/widgets", dto);
    public Task<WidgetDto?> UpdateWidgetAsync(int id, UpdateWidgetDto dto) => PutAsync<WidgetDto>($"api/widgets/{id}", dto);
    public Task<bool> DeleteWidgetAsync(int id) => DeleteAsync($"api/widgets/{id}");
    public Task<WidgetExecutionDto?> ExecuteWidgetAsync(int id) => PostAsync<WidgetExecutionDto>($"api/widgets/{id}/execute", new { });
    public Task<IEnumerable<WidgetExecutionDto>?> GetWidgetHistoryAsync(int id) => GetAsync<IEnumerable<WidgetExecutionDto>>($"api/widgets/{id}/history");

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
}
