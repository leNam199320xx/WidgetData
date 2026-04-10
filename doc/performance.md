# Performance & Optimization

## 📋 Tổng quan

Widget Data được thiết kế để xử lý **hàng triệu rows** với độ trễ thấp thông qua:

1. **Multi-Level Caching** - 3 lớp cache
2. **Database Optimization** - Indexing, query optimization
3. **Async Programming** - Non-blocking I/O
4. **Connection Pooling** - Reuse connections
5. **Lazy Loading & Pagination** - Load on demand
6. **Background Processing** - Hangfire jobs
7. **CDN & Static Assets** - Blazor WASM

---

## 🚀 1. Caching Strategy

### 3-Level Caching Architecture

```
┌─────────────────────────────────────────┐
│  Level 1: In-Memory Cache (IMemoryCache)│
│  TTL: 5-10 minutes                      │
│  Use: Hot data, session data            │
└────────────┬────────────────────────────┘
             │ Miss
┌────────────┴────────────────────────────┐
│  Level 2: Distributed Cache (Redis)     │
│  TTL: 30-60 minutes                     │
│  Use: Shared data, widget results       │
└────────────┬────────────────────────────┘
             │ Miss
┌────────────┴────────────────────────────┐
│  Level 3: Database (SQL Server)         │
│  Source of truth                        │
└─────────────────────────────────────────┘
```

### Implementation

```csharp
public class CacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _redisCache;
    private readonly ApplicationDbContext _db;
    
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        CacheOptions options = null)
    {
        options ??= CacheOptions.Default;
        
        // Level 1: In-Memory
        if (_memoryCache.TryGetValue(key, out T cachedValue))
        {
            return cachedValue;
        }
        
        // Level 2: Redis
        var redisValue = await _redisCache.GetStringAsync(key);
        if (!string.IsNullOrEmpty(redisValue))
        {
            var value = JsonSerializer.Deserialize<T>(redisValue);
            
            // Store in L1
            _memoryCache.Set(key, value, TimeSpan.FromMinutes(options.L1TtlMinutes));
            
            return value;
        }
        
        // Level 3: Database
        var freshValue = await factory();
        
        // Store in L2 (Redis)
        await _redisCache.SetStringAsync(
            key,
            JsonSerializer.Serialize(freshValue),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(options.L2TtlMinutes)
            }
        );
        
        // Store in L1
        _memoryCache.Set(freshValue, key, TimeSpan.FromMinutes(options.L1TtlMinutes));
        
        return freshValue;
    }
}

// Usage
public async Task<WidgetData> GetWidgetDataAsync(int widgetId)
{
    var cacheKey = $"widget_data_{widgetId}";
    
    return await _cacheService.GetOrCreateAsync(
        cacheKey,
        async () => await _widgetService.FetchDataAsync(widgetId),
        new CacheOptions
        {
            L1TtlMinutes = 5,  // In-memory: 5 min
            L2TtlMinutes = 30  // Redis: 30 min
        }
    );
}
```

### Cache Invalidation

```csharp
public class WidgetService
{
    public async Task UpdateWidgetAsync(int widgetId, WidgetDto dto)
    {
        // Update database
        var widget = await _repository.UpdateAsync(widgetId, dto);
        
        // Invalidate cache
        await InvalidateCacheAsync(widgetId);
        
        return widget;
    }
    
    private async Task InvalidateCacheAsync(int widgetId)
    {
        var keys = new[]
        {
            $"widget_{widgetId}",
            $"widget_data_{widgetId}",
            $"widget_config_{widgetId}"
        };
        
        foreach (var key in keys)
        {
            _memoryCache.Remove(key);
            await _redisCache.RemoveAsync(key);
        }
    }
}
```

### Cache-Aside Pattern

```csharp
public async Task<List<Product>> GetProductsAsync()
{
    var cacheKey = "products_all";
    
    // Try cache first
    var cached = await _redisCache.GetStringAsync(cacheKey);
    if (cached != null)
    {
        return JsonSerializer.Deserialize<List<Product>>(cached);
    }
    
    // Cache miss - fetch from DB
    var products = await _db.Products.ToListAsync();
    
    // Update cache
    await _redisCache.SetStringAsync(
        cacheKey,
        JsonSerializer.Serialize(products),
        new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        }
    );
    
    return products;
}
```

---

## 🗄️ 2. Database Optimization

### Indexing Strategy

```sql
-- Widget table indexes
CREATE NONCLUSTERED INDEX IX_Widgets_UserId_IsActive 
ON Widgets(UserId, IsActive) 
INCLUDE (Name, WidgetType, CreatedAt);

CREATE NONCLUSTERED INDEX IX_Widgets_WidgetType 
ON Widgets(WidgetType);

-- DataSource table indexes
CREATE NONCLUSTERED INDEX IX_DataSources_Type 
ON DataSources(SourceType);

-- WidgetExecution table indexes (for history)
CREATE NONCLUSTERED INDEX IX_WidgetExecution_WidgetId_ExecutedAt 
ON WidgetExecutions(WidgetId, ExecutedAt DESC);

-- Composite index for common query
CREATE NONCLUSTERED INDEX IX_WidgetSchedules_NextRun 
ON WidgetSchedules(IsActive, NextRunAt) 
WHERE IsActive = 1;
```

### Query Optimization

**❌ BAD: N+1 Query Problem**
```csharp
// Loads widgets first
var widgets = await _db.Widgets.ToListAsync();

// Then loads data source for each widget (N queries!)
foreach (var widget in widgets)
{
    widget.DataSource = await _db.DataSources.FindAsync(widget.DataSourceId);
}
```

**✅ GOOD: Eager Loading**
```csharp
var widgets = await _db.Widgets
    .Include(w => w.DataSource)
    .Include(w => w.CreatedByUser)
    .Where(w => w.IsActive)
    .ToListAsync();
```

**✅ BETTER: Projection (Select only needed fields)**
```csharp
var widgets = await _db.Widgets
    .Where(w => w.IsActive)
    .Select(w => new WidgetListDto
    {
        Id = w.Id,
        Name = w.Name,
        WidgetType = w.WidgetType,
        DataSourceName = w.DataSource.Name,
        CreatedBy = w.CreatedByUser.UserName
    })
    .ToListAsync();
```

### Pagination

```csharp
public async Task<PagedResult<Widget>> GetWidgetsPagedAsync(int page, int pageSize)
{
    var query = _db.Widgets.Where(w => w.IsActive);
    
    var totalCount = await query.CountAsync();
    
    var items = await query
        .OrderByDescending(w => w.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    
    return new PagedResult<Widget>
    {
        Items = items,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
    };
}
```

### Compiled Queries

```csharp
private static readonly Func<ApplicationDbContext, int, Task<Widget>> _getWidgetByIdCompiled =
    EF.CompileAsyncQuery((ApplicationDbContext db, int id) =>
        db.Widgets
            .Include(w => w.DataSource)
            .FirstOrDefault(w => w.Id == id)
    );

public async Task<Widget> GetWidgetByIdAsync(int id)
{
    return await _getWidgetByIdCompiled(_db, id);
}
```

### Bulk Operations

```csharp
// Instead of multiple INSERTs
// ❌ BAD
foreach (var item in items)
{
    _db.Items.Add(item);
    await _db.SaveChangesAsync(); // Multiple DB calls
}

// ✅ GOOD: Batch insert
_db.Items.AddRange(items);
await _db.SaveChangesAsync(); // Single DB call

// ✅ BETTER: Use EF Core Plus for bulk operations
using EFCore.BulkExtensions;

await _db.BulkInsertAsync(items);
await _db.BulkUpdateAsync(items);
await _db.BulkDeleteAsync(items);
```

---

## ⚡ 3. Async Programming

### Always Use Async

```csharp
// ❌ BAD: Blocking call
public WidgetData GetWidgetData(int id)
{
    var data = _repository.GetById(id).Result; // Blocks thread!
    return data;
}

// ✅ GOOD: Async all the way
public async Task<WidgetData> GetWidgetDataAsync(int id)
{
    var data = await _repository.GetByIdAsync(id);
    return data;
}
```

### Parallel Execution

```csharp
public async Task<DashboardDto> GetDashboardAsync(int userId)
{
    // Execute multiple queries in parallel
    var widgetsTask = _db.Widgets.Where(w => w.UserId == userId).ToListAsync();
    var sourcesTask = _db.DataSources.Where(s => s.UserId == userId).ToListAsync();
    var schedulesTask = _db.WidgetSchedules.Where(s => s.UserId == userId).ToListAsync();
    
    await Task.WhenAll(widgetsTask, sourcesTask, schedulesTask);
    
    return new DashboardDto
    {
        Widgets = await widgetsTask,
        DataSources = await sourcesTask,
        Schedules = await schedulesTask
    };
}
```

### Avoid Async Void

```csharp
// ❌ BAD: Can't catch exceptions
private async void ProcessDataAsync()
{
    await _service.ProcessAsync();
}

// ✅ GOOD: Return Task
private async Task ProcessDataAsync()
{
    await _service.ProcessAsync();
}
```

---

## 🔌 4. Connection Pooling

### SQL Server Connection Pooling

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=WidgetData;Integrated Security=True;Pooling=True;Min Pool Size=5;Max Pool Size=100;Connection Timeout=30;"
  }
}
```

### HTTP Client Factory

```csharp
// ❌ BAD: Creates new HttpClient for each request
public class DataSourceService
{
    public async Task<string> FetchDataAsync(string url)
    {
        using var client = new HttpClient(); // Socket exhaustion!
        return await client.GetStringAsync(url);
    }
}

// ✅ GOOD: Use IHttpClientFactory
public class DataSourceService
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    public DataSourceService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    
    public async Task<string> FetchDataAsync(string url)
    {
        var client = _httpClientFactory.CreateClient();
        return await client.GetStringAsync(url);
    }
}

// Register in Program.cs
builder.Services.AddHttpClient();
```

---

## 📊 5. Data Processing Optimization

### Stream Processing for Large Files

```csharp
// ❌ BAD: Load entire file into memory
public async Task<List<Record>> ReadCsvAsync(string path)
{
    var lines = await File.ReadAllLinesAsync(path); // Could be GB!
    return lines.Select(line => ParseRecord(line)).ToList();
}

// ✅ GOOD: Stream processing
public async IAsyncEnumerable<Record> ReadCsvStreamAsync(string path)
{
    using var reader = new StreamReader(path);
    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
    
    await foreach (var record in csv.GetRecordsAsync<Record>())
    {
        yield return record;
    }
}

// Usage
await foreach (var record in ReadCsvStreamAsync("large-file.csv"))
{
    await ProcessRecordAsync(record); // Process one at a time
}
```

### Batch Processing

```csharp
public async Task ProcessLargeDatasetAsync(List<Record> records)
{
    const int batchSize = 1000;
    
    for (int i = 0; i < records.Count; i += batchSize)
    {
        var batch = records.Skip(i).Take(batchSize).ToList();
        
        await _db.BulkInsertAsync(batch);
        
        // Optional: Progress reporting
        var progress = (i + batchSize) / (double)records.Count * 100;
        await _hubContext.Clients.All.SendAsync("ProgressUpdate", progress);
    }
}
```

---

## 🎯 6. Blazor Performance

### Virtualization for Large Lists

```razor
@using Microsoft.AspNetCore.Components.Web.Virtualization

<Virtualize Items="@widgets" Context="widget">
    <WidgetCard Widget="@widget" />
</Virtualize>

@code {
    private List<Widget> widgets = new();
    
    protected override async Task OnInitializedAsync()
    {
        widgets = await WidgetService.GetAllAsync();
    }
}
```

### Lazy Loading

```razor
@page "/widgets/{id:int}"

<MudCard>
    <MudCardHeader>
        <MudText Typo="Typo.h6">@widget?.Name</MudText>
    </MudCardHeader>
    <MudCardContent>
        @if (widgetData == null)
        {
            <MudProgressCircular Indeterminate="true" />
        }
        else
        {
            <WidgetChart Data="@widgetData" />
        }
    </MudCardContent>
</MudCard>

@code {
    [Parameter] public int Id { get; set; }
    
    private Widget widget;
    private WidgetData widgetData;
    
    protected override async Task OnInitializedAsync()
    {
        // Load widget metadata first (fast)
        widget = await WidgetService.GetByIdAsync(Id);
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Load data after render (lazy)
            widgetData = await WidgetService.GetDataAsync(Id);
            StateHasChanged();
        }
    }
}
```

### SignalR Optimization

```csharp
// ✅ Send to specific group instead of all clients
public class WidgetHub : Hub
{
    public async Task JoinWidgetGroup(int widgetId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"widget_{widgetId}");
    }
    
    public async Task BroadcastWidgetUpdate(int widgetId, object data)
    {
        // Only send to users watching this widget
        await Clients.Group($"widget_{widgetId}")
            .SendAsync("WidgetUpdated", data);
    }
}
```

---

## 📈 7. Monitoring Performance

### Application Insights Metrics

```csharp
public class WidgetService
{
    private readonly TelemetryClient _telemetry;
    
    public async Task<WidgetData> GetDataAsync(int widgetId)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var data = await FetchDataAsync(widgetId);
            
            // Track success
            _telemetry.TrackMetric("WidgetDataFetch.Duration", stopwatch.ElapsedMilliseconds);
            _telemetry.TrackMetric("WidgetDataFetch.RowCount", data.Rows.Count);
            
            return data;
        }
        catch (Exception ex)
        {
            _telemetry.TrackException(ex);
            throw;
        }
    }
}
```

### Custom Performance Counters

```csharp
public class PerformanceMonitor
{
    private static readonly Counter _requestCounter = 
        Metrics.CreateCounter("widget_requests_total", "Total widget requests");
    
    private static readonly Histogram _requestDuration = 
        Metrics.CreateHistogram("widget_request_duration_seconds", "Widget request duration");
    
    public async Task<T> TrackAsync<T>(Func<Task<T>> action)
    {
        _requestCounter.Inc();
        
        using (_requestDuration.NewTimer())
        {
            return await action();
        }
    }
}
```

---

## 🎯 Performance Targets

| Metric | Target | Current |
|--------|--------|---------|
| **API Response Time** | < 200ms (p95) | 150ms |
| **Widget Refresh** | < 1s | 800ms |
| **Dashboard Load** | < 2s | 1.5s |
| **Concurrent Users** | 1000+ | Tested 500 |
| **Database Query** | < 100ms | 80ms |
| **Cache Hit Rate** | > 80% | 85% |
| **Memory Usage** | < 2GB | 1.2GB |

---

## 🔍 Performance Checklist

### Code Review
- [ ] All database queries use async
- [ ] No N+1 query problems
- [ ] Proper indexing on queried columns
- [ ] Pagination for large datasets
- [ ] Cache frequently accessed data
- [ ] Use projections (Select) instead of full entities
- [ ] Dispose resources properly (IDisposable)

### Production
- [ ] Connection pooling enabled
- [ ] Redis distributed cache configured
- [ ] CDN for static assets (Blazor WASM)
- [ ] Compression enabled (gzip/brotli)
- [ ] Health checks monitoring
- [ ] Application Insights enabled
- [ ] Regular performance testing

---

← [Quay lại INDEX](INDEX.md)
