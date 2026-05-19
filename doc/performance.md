# Hiệu năng & Tối ưu hóa

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

## 🚀 1. Chiến lược Cache

### Kiến trúc Cache 3 cấp

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

### Triển khai

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
        
        // Tầng 1: In-Memory
        if (_memoryCache.TryGetValue(key, out T cachedValue))
        {
            return cachedValue;
        }
        
        // Tầng 2: Redis
        var redisValue = await _redisCache.GetStringAsync(key);
        if (!string.IsNullOrEmpty(redisValue))
        {
            var value = JsonSerializer.Deserialize<T>(redisValue);
            
            // Lưu vào L1
            _memoryCache.Set(key, value, TimeSpan.FromMinutes(options.L1TtlMinutes));
            
            return value;
        }
        
        // Tầng 3: Database
        var freshValue = await factory();
        
        // Lưu vào L2 (Redis)
        await _redisCache.SetStringAsync(
            key,
            JsonSerializer.Serialize(freshValue),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(options.L2TtlMinutes)
            }
        );
        
        // Lưu vào L1
        _memoryCache.Set(freshValue, key, TimeSpan.FromMinutes(options.L1TtlMinutes));
        
        return freshValue;
    }
}

// Cách dùng
public async Task<WidgetData> GetWidgetDataAsync(int widgetId)
{
    var cacheKey = $"widget_data_{widgetId}";
    
    return await _cacheService.GetOrCreateAsync(
        cacheKey,
        async () => await _widgetService.FetchDataAsync(widgetId),
        new CacheOptions
        {
            L1TtlMinutes = 5,  // In-memory: 5 phút
            L2TtlMinutes = 30  // Redis: 30 phút
        }
    );
}
```

### Hủy Cache

```csharp
public class WidgetService
{
    public async Task UpdateWidgetAsync(int widgetId, WidgetDto dto)
    {
        // Cập nhật database
        var widget = await _repository.UpdateAsync(widgetId, dto);
        
        // Xóa cache
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

### Mẫu Cache-Aside

```csharp
public async Task<List<Product>> GetProductsAsync()
{
    var cacheKey = "products_all";
    
    // Thử cache trước
    var cached = await _redisCache.GetStringAsync(cacheKey);
    if (cached != null)
    {
        return JsonSerializer.Deserialize<List<Product>>(cached);
    }
    
    // Cache miss - lấy từ DB
    var products = await _db.Products.ToListAsync();
    
    // Cập nhật cache
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

## 🗄️ 2. Tối ưu hóa Database

### Chiến lược Index

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

### Tối ưu hóa Truy vấn

**❌ SAI: Vấn đề N+1 Query**
```csharp
// Tải widgets trước
var widgets = await _db.Widgets.ToListAsync();

// Rồi tải data source cho từng widget (N query!)
foreach (var widget in widgets)
{
    widget.DataSource = await _db.DataSources.FindAsync(widget.DataSourceId);
}
```

**✅ ĐÚNG: Eager Loading**
```csharp
var widgets = await _db.Widgets
    .Include(w => w.DataSource)
    .Include(w => w.CreatedByUser)
    .Where(w => w.IsActive)
    .ToListAsync();
```

**✅ TỐT HƠN: Projection (chỉ lấy các trường cần thiết)**
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

### Phân trang

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

### Truy vấn đã biên dịch

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

### Thao tác hàng loạt

```csharp
// Thay vì nhiều lần INSERT
// ❌ SAI
foreach (var item in items)
{
    _db.Items.Add(item);
    await _db.SaveChangesAsync(); // Nhiều lần gọi DB
}

// ✅ ĐÚNG: Batch insert
_db.Items.AddRange(items);
await _db.SaveChangesAsync(); // Một lần gọi DB

// ✅ TỐT HƠN: Dùng EF Core Plus cho bulk operations
using EFCore.BulkExtensions;

await _db.BulkInsertAsync(items);
await _db.BulkUpdateAsync(items);
await _db.BulkDeleteAsync(items);
```

---

## ⚡ 3. Lập trình Bất đồng bộ

### Luôn dùng Async

```csharp
// ❌ SAI: Gọi chặn luồng
public WidgetData GetWidgetData(int id)
{
    var data = _repository.GetById(id).Result; // Chặn luồng!
    return data;
}

// ✅ ĐÚNG: Async toàn bộ
public async Task<WidgetData> GetWidgetDataAsync(int id)
{
    var data = await _repository.GetByIdAsync(id);
    return data;
}
```

### Thực thi Song song

```csharp
public async Task<DashboardDto> GetDashboardAsync(int userId)
{
    // Thực thi nhiều query song song
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

### Tránh Async Void

```csharp
// ❌ SAI: Không thể bắt exception
private async void ProcessDataAsync()
{
    await _service.ProcessAsync();
}

// ✅ ĐÚNG: Trả về Task
private async Task ProcessDataAsync()
{
    await _service.ProcessAsync();
}
```

---

## 🔌 4. Connection Pooling

### Connection Pooling với SQL Server

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=WidgetData;Integrated Security=True;Pooling=True;Min Pool Size=5;Max Pool Size=100;Connection Timeout=30;"
  }
}
```

### HTTP Client Factory

```csharp
// ❌ SAI: Tạo HttpClient mới cho mỗi request
public class DataSourceService
{
    public async Task<string> FetchDataAsync(string url)
    {
        using var client = new HttpClient(); // Socket exhaustion!
        return await client.GetStringAsync(url);
    }
}

// ✅ ĐÚNG: Dùng IHttpClientFactory
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

// Đăng ký trong Program.cs
builder.Services.AddHttpClient();
```

---

## 📊 5. Tối ưu hóa Xử lý Dữ liệu

### Xử lý Luồng cho File lớn

```csharp
// ❌ SAI: Tải toàn bộ file vào bộ nhớ
public async Task<List<Record>> ReadCsvAsync(string path)
{
    var lines = await File.ReadAllLinesAsync(path); // Có thể vài GB!
    return lines.Select(line => ParseRecord(line)).ToList();
}

// ✅ ĐÚNG: Xử lý theo luồng
public async IAsyncEnumerable<Record> ReadCsvStreamAsync(string path)
{
    using var reader = new StreamReader(path);
    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
    
    await foreach (var record in csv.GetRecordsAsync<Record>())
    {
        yield return record;
    }
}

// Cách dùng
await foreach (var record in ReadCsvStreamAsync("large-file.csv"))
{
    await ProcessRecordAsync(record); // Xử lý từng bản ghi một
}
```

### Xử lý Theo lô

```csharp
public async Task ProcessLargeDatasetAsync(List<Record> records)
{
    const int batchSize = 1000;
    
    for (int i = 0; i < records.Count; i += batchSize)
    {
        var batch = records.Skip(i).Take(batchSize).ToList();
        
        await _db.BulkInsertAsync(batch);
        
        // Tùy chọn: Báo cáo tiến độ
        var progress = (i + batchSize) / (double)records.Count * 100;
        await _hubContext.Clients.All.SendAsync("ProgressUpdate", progress);
    }
}
```

---

## 🎯 6. Hiệu năng Blazor

### Ảo hóa cho Danh sách lớn

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

### Tải Lazy

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
        // Tải metadata widget trước (nhanh)
        widget = await WidgetService.GetByIdAsync(Id);
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Tải dữ liệu sau khi render (lazy)
            widgetData = await WidgetService.GetDataAsync(Id);
            StateHasChanged();
        }
    }
}
```

### Tối ưu hóa SignalR

```csharp
// ✅ Gửi đến nhóm cụ thể thay vì tất cả client
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

## 📈 7. Giám sát Hiệu năng

### Metrics Application Insights

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
            
            // Ghi nhận thành công
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

### Bộ đếm Hiệu năng tùy chỉnh

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

## 🎯 Mục tiêu Hiệu năng

| Chỉ số | Mục tiêu | Hiện tại |
|--------|--------|---------|
| **Thời gian phản hồi API** | < 200ms (p95) | 150ms |
| **Làm mới Widget** | < 1s | 800ms |
| **Tải Dashboard** | < 2s | 1.5s |
| **Người dùng đồng thời** | 1000+ | Đã test 500 |
| **Query Database** | < 100ms | 80ms |
| **Tỷ lệ Cache Hit** | > 80% | 85% |
| **Dung lượng bộ nhớ** | < 2GB | 1.2GB |

---

## 🔍 Danh sách kiểm tra Hiệu năng

### Rà soát Code
- [ ] Tất cả query database dùng async
- [ ] Không có vấn đề N+1 query
- [ ] Index đúng trên các cột được truy vấn
- [ ] Phân trang cho tập dữ liệu lớn
- [ ] Cache dữ liệu thường xuyên truy cập
- [ ] Dùng projection (Select) thay vì entity đầy đủ
- [ ] Giải phóng tài nguyên đúng cách (IDisposable)

### Sản xuất
- [ ] Connection pooling được bật
- [ ] Redis distributed cache đã cấu hình
- [ ] CDN cho static assets (Blazor WASM)
- [ ] Nén dữ liệu được bật (gzip/brotli)
- [ ] Giám sát health checks
- [ ] Application Insights được bật
- [ ] Kiểm tra hiệu năng định kỳ

---

← [Quay lại INDEX](INDEX.md)
