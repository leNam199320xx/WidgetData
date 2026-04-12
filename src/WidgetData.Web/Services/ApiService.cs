using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WidgetData.Application.DTOs;

namespace WidgetData.Web.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private readonly TokenStore _tokenStore;

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
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private async Task<T?> PostAsync<T>(string url, object data)
    {
        ApplyToken();
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync(url, content);
        if (!response.IsSuccessStatusCode) return default;
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public Task<AuthResponseDto?> LoginAsync(LoginDto dto) => PostAsync<AuthResponseDto>("api/auth/login", dto);
    public Task<IEnumerable<WidgetDto>?> GetWidgetsAsync() => GetAsync<IEnumerable<WidgetDto>>("api/widgets");
    public Task<IEnumerable<DataSourceDto>?> GetDataSourcesAsync() => GetAsync<IEnumerable<DataSourceDto>>("api/datasources");
    public Task<IEnumerable<WidgetScheduleDto>?> GetSchedulesAsync() => GetAsync<IEnumerable<WidgetScheduleDto>>("api/schedules");
    public Task<DashboardStatsDto?> GetDashboardStatsAsync() => GetAsync<DashboardStatsDto>("api/dashboard/stats");
    public Task<IEnumerable<UserDto>?> GetUsersAsync() => GetAsync<IEnumerable<UserDto>>("api/users");
}
