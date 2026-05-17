namespace WidgetData.Infrastructure.Data.Json.Repositories;

/// <summary>
/// Base class for JSON-based repositories
/// </summary>
public abstract class BaseJsonRepository<T> : IJsonRepository<T> where T : class
{
    protected readonly JsonDataProvider _jsonProvider;
    protected readonly string _subdirectory;

    protected BaseJsonRepository(JsonDataProvider jsonProvider, string subdirectory)
    {
        _jsonProvider = jsonProvider ?? throw new ArgumentNullException(nameof(jsonProvider));
        _subdirectory = subdirectory ?? throw new ArgumentNullException(nameof(subdirectory));
    }

    /// <summary>
    /// Get entity filename based on ID
    /// </summary>
    protected virtual string GetFileName(int id) => $"{id}.json";

    /// <summary>
    /// Get index file name for collection
    /// </summary>
    protected virtual string GetIndexFileName() => "index.json";

    /// <summary>
    /// Load entity index from cache
    /// </summary>
    protected async Task<Dictionary<int, string>> LoadIndexAsync()
    {
        var indexFile = GetIndexFileName();
        var index = await _jsonProvider.LoadAsync<Dictionary<int, string>>(_subdirectory, indexFile);
        return index ?? new Dictionary<int, string>();
    }

    /// <summary>
    /// Save entity index
    /// </summary>
    protected async Task SaveIndexAsync(Dictionary<int, string> index)
    {
        var indexFile = GetIndexFileName();
        await _jsonProvider.SaveAsync(index, _subdirectory, indexFile);
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        var fileName = GetFileName(id);
        return await _jsonProvider.LoadAsync<T>(_subdirectory, fileName);
    }

    public async Task<List<T>> GetAllAsync()
    {
        return await _jsonProvider.LoadAllAsync<T>(_subdirectory);
    }

    public virtual async Task<List<T>> GetByTenantAsync(int? tenantId)
    {
        var all = await GetAllAsync();
        if (tenantId == null)
            return all;

        // Default implementation - override in derived classes for tenant filtering
        return all;
    }

    public async Task<T> CreateAsync(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var id = GetEntityId(entity);
        var fileName = GetFileName(id);
        await _jsonProvider.SaveAsync(entity, _subdirectory, fileName);

        // Update index
        var index = await LoadIndexAsync();
        index[id] = fileName;
        await SaveIndexAsync(index);

        return entity;
    }

    public async Task<T> UpdateAsync(T entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var id = GetEntityId(entity);
        var fileName = GetFileName(id);
        await _jsonProvider.SaveAsync(entity, _subdirectory, fileName);

        return entity;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var fileName = GetFileName(id);
        var deleted = _jsonProvider.Delete(_subdirectory, fileName);

        if (deleted)
        {
            // Update index
            var index = await LoadIndexAsync();
            index.Remove(id);
            await SaveIndexAsync(index);
        }

        return deleted;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var fileName = GetFileName(id);
        return _jsonProvider.Exists(_subdirectory, fileName);
    }

    /// <summary>
    /// Extract ID from entity - override in derived classes
    /// </summary>
    protected abstract int GetEntityId(T entity);
}
