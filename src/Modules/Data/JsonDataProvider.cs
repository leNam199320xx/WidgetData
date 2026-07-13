using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

namespace WidgetData.Data;

public class JsonDataProvider
{
    private readonly string _dataDirectory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = true
    };

    public JsonDataProvider(string? baseDataDirectory = null)
    {
        _dataDirectory = baseDataDirectory ?? Path.Combine(AppContext.BaseDirectory, "data");
        EnsureDataDirectoryExists();
    }

    private void EnsureDataDirectoryExists()
    {
        if (!Directory.Exists(_dataDirectory))
            Directory.CreateDirectory(_dataDirectory);
    }

    public string GetFilePath(string subdirectory, string fileName)
    {
        var dir = Path.Combine(_dataDirectory, subdirectory);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        return Path.Combine(dir, fileName);
    }

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

    public async Task<List<T>> LoadAllAsync<T>(string subdirectory) where T : class
    {
        var dir = Path.Combine(_dataDirectory, subdirectory);
        if (!Directory.Exists(dir))
            return new List<T>();

        var jsonFiles = Directory.GetFiles(dir, "*.json");

        var tasks = jsonFiles.Select(async filePath =>
        {
            try
            {
                await using var stream = File.OpenRead(filePath);
                return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load JSON from {filePath}", ex);
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.Where(item => item != null).Cast<T>().ToList();
    }

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

    public bool Exists(string subdirectory, string fileName)
    {
        var filePath = GetFilePath(subdirectory, fileName);
        return File.Exists(filePath);
    }

    public string[] GetFiles(string subdirectory, string pattern = "*.json")
    {
        var dir = Path.Combine(_dataDirectory, subdirectory);
        if (!Directory.Exists(dir))
            return Array.Empty<string>();
        return Directory.GetFiles(dir, pattern);
    }
}
