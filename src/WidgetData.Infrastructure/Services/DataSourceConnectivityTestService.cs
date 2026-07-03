using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using WidgetData.Application.Interfaces;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Enums;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Services;

namespace WidgetData.Infrastructure.Services;

public class DataSourceConnectivityTestService : IDataSourceConnectivityTestService
{
    private readonly IDataSourceRepository _repo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEnumerable<IDataSourceValidator> _validators;
    private readonly ILogger _logger;

    public DataSourceConnectivityTestService(IDataSourceRepository repo, IHttpClientFactory httpClientFactory,
        IEnumerable<IDataSourceValidator> validators, ILogger logger)
    {
        _repo = repo;
        _httpClientFactory = httpClientFactory;
        _validators = validators;
        _logger = logger;
    }

    public async Task<string> TestConnectionAsync(int id)
    {
        var ds = await _repo.GetByIdAsync(id);
        if (ds == null) return "Data source not found";

        string result;
        try
        {
            result = ds.SourceType switch
            {
                DataSourceType.RestApi => await TestRestApiAsync(ds.ApiEndpoint, ds.ApiKey),
                DataSourceType.Csv or DataSourceType.Excel or DataSourceType.Json
                    => await TestFileSourceAsync(ds.FileStoragePath ?? ds.ConnectionString, ds.SourceType, _validators),
                _ => $"Source type {ds.SourceType} is not supported for connection test."
            };
        }
        catch (Exception ex)
        {
            result = $"Connection failed: {ex.Message}";
        }

        ds.LastTestedAt = DateTime.UtcNow;
        ds.LastTestResult = result;
        await _repo.UpdateAsync(ds);
        return result;
    }

    private async Task<string> TestRestApiAsync(string? endpoint, string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            return "Connection failed: API endpoint is empty";
        using var http = _httpClientFactory.CreateClient();
        if (!string.IsNullOrWhiteSpace(apiKey))
            http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        var response = await http.GetAsync(endpoint);
        return response.IsSuccessStatusCode
            ? $"Connection successful (HTTP {(int)response.StatusCode})"
            : $"Connection failed: HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
    }

    private static async Task<string> TestFileSourceAsync(string? filePath, DataSourceType sourceType, IEnumerable<IDataSourceValidator> validators)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return $"Connection failed: {sourceType} file path is empty";
        if (!File.Exists(filePath))
            return $"Connection failed: {sourceType} file not found: {filePath}";

        var validator = validators.FirstOrDefault(v => v.CanValidate(sourceType));
        if (validator != null)
        {
            var ds = new DataSource { ConnectionString = filePath, FileStoragePath = filePath, SourceType = sourceType };
            await validator.ValidateAsync(ds);
        }

        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _ = stream.Length;
            return "Connection successful";
        }
        catch (Exception ex)
        {
            return $"Connection failed: cannot read {sourceType} file - {ex.Message}";
        }
    }
}