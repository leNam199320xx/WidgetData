using System.Text.Json;
using System.Text.Json.Serialization;

namespace WidgetData.Infrastructure.Data.Json;

/// <summary>
/// JSON Data Provider - handles all JSON file I/O operations
/// </summary>
public class JsonDataProvider
{
    private readonly string _dataDirectory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public JsonDataProvider(string? baseDataDirectory = null)
    {
        _dataDirectory = baseDataDirectory ?? Path.Combine(AppContext.BaseDirectory, "data");
        EnsureDataDirectoryExists();
    }

    /// <summary>
    /// Ensure data directory exists
    /// </summary>
    private void EnsureDataDirectoryExists()
    {
        if (!Directory.Exists(_dataDirectory))
            Directory.CreateDirectory(_dataDirectory);
    }

    /// <summary>
    /// Get full path for a data file
    /// </summary>
    public string GetFilePath(string subdirectory, string fileName)
    {
        var dir = Path.Combine(_dataDirectory, subdirectory);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        return Path.Combine(dir, fileName);
    }

    /// <summary>
    /// Load a JSON file and deserialize to type T
    /// </summary>
    public async Task<T?> LoadAsync<T>(string subdirectory, string fileName) where T : class
    {
        var filePath = GetFilePath(subdirectory, fileName);
        if (!File.Exists(filePath))
            return null;

        try
        {
            await using var stream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load JSON from {filePath}", ex);
        }
    }

    /// <summary>
    /// Load all JSON files from a subdirectory as collection
    /// </summary>
    public async Task<List<T>> LoadAllAsync<T>(string subdirectory) where T : class
    {
        var dir = Path.Combine(_dataDirectory, subdirectory);
        if (!Directory.Exists(dir))
            return new List<T>();

        var result = new List<T>();
        var jsonFiles = Directory.GetFiles(dir, "*.json");

        foreach (var filePath in jsonFiles)
        {
            try
            {
                await using var stream = File.OpenRead(filePath);
                var item = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions);
                if (item != null)
                    result.Add(item);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load JSON from {filePath}", ex);
            }
        }

        return result;
    }

    /// <summary>
    /// Save object as JSON file
    /// </summary>
    public async Task SaveAsync<T>(T data, string subdirectory, string fileName) where T : class
    {
        var filePath = GetFilePath(subdirectory, fileName);
        
        try
        {
            await using var stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, data, JsonOptions);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save JSON to {filePath}", ex);
        }
    }

    /// <summary>
    /// Delete a JSON file
    /// </summary>
    public bool Delete(string subdirectory, string fileName)
    {
        var filePath = GetFilePath(subdirectory, fileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Check if file exists
    /// </summary>
    public bool Exists(string subdirectory, string fileName)
    {
        var filePath = GetFilePath(subdirectory, fileName);
        return File.Exists(filePath);
    }

    /// <summary>
    /// Get all files in subdirectory
    /// </summary>
    public string[] GetFiles(string subdirectory, string pattern = "*.json")
    {
        var dir = Path.Combine(_dataDirectory, subdirectory);
        if (!Directory.Exists(dir))
            return Array.Empty<string>();
        return Directory.GetFiles(dir, pattern);
    }
}
