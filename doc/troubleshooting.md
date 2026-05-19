# Xử lý sự cố & Câu hỏi thường gặp

## 📋 Các vấn đề thường gặp

### 🔴 Lỗi kết nối Database

**Lỗi:** `Cannot open database "WidgetData" requested by the login`

**Nguyên nhân:**
- Database chưa tồn tại
- Connection string không đúng
- SQL Server chưa chạy

**Giải pháp:**
```bash
# 1. Kiểm tra SQL Server đang chạy
Get-Service MSSQLSERVER

# 2. Kiểm tra kết nối
sqlcmd -S localhost -E -Q "SELECT @@VERSION"

# 3. Tạo database nếu chưa tồn tại
dotnet ef database update --project src/WidgetData.Infrastructure

# 4. Kiểm tra connection string
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=WidgetData;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

---

**Lỗi:** `Login failed for user 'IIS APPPOOL\WidgetDataAppPool'`

**Giải pháp:**
```sql
-- Cấp quyền cho user IIS AppPool
USE WidgetData;
CREATE USER [IIS APPPOOL\WidgetDataAppPool] FOR LOGIN [IIS APPPOOL\WidgetDataAppPool];
ALTER ROLE db_datareader ADD MEMBER [IIS APPPOOL\WidgetDataAppPool];
ALTER ROLE db_datawriter ADD MEMBER [IIS APPPOOL\WidgetDataAppPool];
```

---

### 🔴 Sự cố EF Core Migration

**Lỗi:** `Unable to create an object of type 'ApplicationDbContext'`

**Giải pháp:**
```bash
# Chỉ định startup project
dotnet ef migrations add InitialCreate \
  --project src/WidgetData.Infrastructure \
  --startup-project src/WidgetData.Web

dotnet ef database update \
  --project src/WidgetData.Infrastructure \
  --startup-project src/WidgetData.Web
```

---

**Lỗi:** `The migration '20260410_Initial' has already been applied`

**Giải pháp:**
```bash
# Xóa migration cuối cùng
dotnet ef migrations remove --project src/WidgetData.Infrastructure

# Hoặc áp dụng lại bắt buộc
dotnet ef database update 0 --project src/WidgetData.Infrastructure
dotnet ef database update --project src/WidgetData.Infrastructure
```

---

### 🔴 Sự cố Hangfire

**Lỗi:** `Hangfire dashboard returns 404`

**Giải pháp:**
```csharp
// Đảm bảo middleware được đăng ký đúng thứ tự
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.MapControllers();
```

---

**Lỗi:** `BackgroundJob is not processed`

**Giải pháp:**
```csharp
// 1. Kiểm tra Hangfire Server đang chạy
services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 5;
});

// 2. Kiểm tra job đã được đưa vào hàng đợi
var jobId = BackgroundJob.Enqueue<MyService>(x => x.DoWork());
Console.WriteLine($"Job ID: {jobId}"); // Không được null

// 3. Kiểm tra logs Hangfire
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Hangfire", LogEventLevel.Debug)
    .WriteTo.Console()
    .CreateLogger();
```

---

### 🔴 Sự cố kết nối Redis

**Lỗi:** `It was not possible to connect to the redis server(s)`

**Giải pháp:**
```bash
# 1. Kiểm tra Redis đang chạy
redis-cli ping
# Kết quả mong đợi: PONG

# 2. Kiểm tra connection string
{
  "Redis": {
    "Configuration": "localhost:6379",
    "InstanceName": "WidgetData:"
  }
}

# 3. Cho phép kết nối từ xa (nếu cần)
# Chỉnh sửa redis.conf
bind 0.0.0.0
protected-mode no

# Khởi động lại Redis
redis-server --service-stop
redis-server --service-start
```

---

### 🔴 Sự cố Blazor

**Lỗi:** `Blazor app shows blank page`

**Giải pháp:**
```bash
# 1. Kiểm tra console trình duyệt để xem lỗi
# 2. Xác minh _framework/blazor.webassembly.js đã được tải
# 3. Xóa cache trình duyệt
# 4. Build lại từ đầu
dotnet clean
dotnet build
```

---

**Lỗi:** `SignalR connection failed`

**Giải pháp:**
```csharp
// Bật log chi tiết cho SignalR
services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});

// Phía client (Blazor)
_hubConnection = new HubConnectionBuilder()
    .WithUrl(Navigation.ToAbsoluteUri("/widgetHub"))
    .ConfigureLogging(logging =>
    {
        logging.SetMinimumLevel(LogLevel.Debug);
    })
    .Build();
```

---

### 🔴 Sự cố hiệu năng

**Sự cố:** Thực thi widget chậm (> 5 giây)

**Chẩn đoán:**
```csharp
// Thêm log đo thời gian
var stopwatch = Stopwatch.StartNew();

_logger.LogInformation("Step 1: Extract started");
var data = await ExtractDataAsync();
_logger.LogInformation("Step 1 completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

stopwatch.Restart();
_logger.LogInformation("Step 2: Transform started");
var transformed = await TransformDataAsync(data);
_logger.LogInformation("Step 2 completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
```

**Giải pháp:**
```csharp
// 1. Thêm indexes
CREATE NONCLUSTERED INDEX IX_Orders_Date ON Orders(OrderDate);

// 2. Sử dụng phân trang
var query = _context.Orders
    .Where(o => o.OrderDate >= startDate)
    .Take(1000); // Giới hạn số dòng

// 3. Bật caching
var cacheKey = $"widget_{widgetId}";
var data = await _cache.GetOrCreateAsync(cacheKey, async entry =>
{
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
    return await FetchDataAsync();
});

// 4. Sử dụng async đúng cách
// ❌ SAI
var result = _service.GetDataAsync().Result; // Chặn luồng!

// ✅ ĐÚNG
var result = await _service.GetDataAsync();
```

---

### 🔴 Sự cố tải file lên

**Lỗi:** `Request body too large`

**Giải pháp:**
```csharp
// Tăng kích thước request tối đa
services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100_000_000; // 100 MB
});

// Trong Controller
[RequestSizeLimit(100_000_000)]
[HttpPost("upload")]
public async Task<IActionResult> Upload(IFormFile file)
{
    // ...
}

// IIS web.config
<system.webServer>
  <security>
    <requestFiltering>
      <requestLimits maxAllowedContentLength="104857600" />
    </requestFiltering>
  </security>
</system.webServer>
```

---

## ❓ Câu hỏi thường gặp

### Hỏi: Làm thế nào để đặt lại mật khẩu admin?

**Trả lời:** Chạy script SQL sau:
```sql
-- Tìm người dùng
SELECT * FROM AspNetUsers WHERE Email = 'admin@widgetdata.com';

-- Cập nhật mật khẩu (hash cho "Admin@123")
UPDATE AspNetUsers
SET PasswordHash = 'AQAAAAIAAYagAAAAEL...' -- Sử dụng hash đúng
WHERE Email = 'admin@widgetdata.com';
```

Hoặc dùng công cụ CLI:
```bash
dotnet run --project src/WidgetData.Web -- reset-password admin@widgetdata.com
```

---

### Hỏi: Làm thế nào để bật debug logging?

**Trả lời:** Cập nhật `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information",
      "WidgetData": "Trace"
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  }
}
```

---

### Hỏi: Làm thế nào để xóa cache thủ công?

**Trả lời:**
```bash
# Redis
redis-cli FLUSHDB

# Cache trong bộ nhớ (khởi động lại app)
iisreset

# Hoặc qua API
curl -X POST https://localhost:7001/api/cache/clear -H "Authorization: Bearer YOUR_TOKEN"
```

---

### Hỏi: Widget hiển thị "Không có dữ liệu" nhưng query trả về dữ liệu?

**Trả lời:** Kiểm tra:
```csharp
// 1. Xác minh query thực sự trả về dữ liệu
var testResult = await _db.ExecuteQueryAsync("SELECT * FROM Orders");
_logger.LogInformation("Query returned {Count} rows", testResult.Rows.Count);

// 2. Kiểm tra ánh xạ biến đầu ra
{
  "step_id": 1,
  "output": "orders" // ✅ Đảm bảo điều này khớp với đầu vào của bước tiếp theo
}

// 3. Kiểm tra ánh xạ kiểu dữ liệu
// Nếu cột là DECIMAL, đảm bảo không bị cắt bớt
```

---

### Hỏi: Làm thế nào để lên lịch widget chạy mỗi 5 phút?

**Trả lời:**
```json
{
  "widget_id": 123,
  "schedule": {
    "cron": "*/5 * * * *",
    "timezone": "UTC"
  }
}
```

Định dạng Cron: `phút giờ ngày tháng ngày-trong-tuần`

Ví dụ:
- Mỗi 5 phút: `*/5 * * * *`
- Mỗi giờ: `0 * * * *`
- Hàng ngày lúc 2 giờ sáng: `0 2 * * *`
- Thứ Hai lúc 9 giờ sáng: `0 9 * * 1`

---

### Hỏi: Làm thế nào để xuất dữ liệu widget ra Excel?

**Trả lời:**
```csharp
[HttpGet("widgets/{id}/export/excel")]
public async Task<IActionResult> ExportExcel(int id)
{
    var data = await _widgetService.GetDataAsync(id);
    
    using var package = new ExcelPackage();
    var worksheet = package.Workbook.Worksheets.Add("Widget Data");
    
    // Nạp dữ liệu
    worksheet.Cells["A1"].LoadFromDataTable(data, true);
    
    // Tự động điều chỉnh cột
    worksheet.Cells.AutoFitColumns();
    
    var bytes = await package.GetAsByteArrayAsync();
    return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
        $"widget_{id}.xlsx");
}
```

---

### Hỏi: Làm thế nào để thêm loại data source mới?

**Trả lời:**
```csharp
// 1. Tạo connector
public class CustomApiConnector : IDataSourceConnector
{
    public async Task<DataTable> ExecuteQueryAsync(string endpoint, Dictionary<string, object> parameters)
    {
        var response = await _httpClient.GetAsync(endpoint);
        var json = await response.Content.ReadAsStringAsync();
        
        // Phân tích JSON thành DataTable
        var data = JsonConvert.DeserializeObject<DataTable>(json);
        return data;
    }
}

// 2. Đăng ký
services.AddScoped<IDataSourceConnector, CustomApiConnector>();

// 3. Sử dụng trong widget
{
  "source": {
    "type": "custom_api",
    "endpoint": "https://api.example.com/data"
  }
}
```

---

### Hỏi: Mức sử dụng bộ nhớ liên tục tăng

**Trả lời:** Các nguyên nhân thường gặp:

```csharp
// 1. Không dispose DbContext
// ❌ SAI
public class MyService
{
    private readonly ApplicationDbContext _context; // Vòng đời Singleton!
}

// ✅ ĐÚNG
public class MyService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    
    public async Task DoWorkAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        // Sử dụng context
    }
}

// 2. Cache quá nhiều dữ liệu
// Đặt thời gian hết hạn
_cache.Set(key, data, new MemoryCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
    SlidingExpiration = TimeSpan.FromMinutes(2)
});

// 3. Không dispose HttpClient
// Sử dụng IHttpClientFactory
services.AddHttpClient();
```

---

### Hỏi: Làm thế nào để chuyển đổi từ môi trường phát triển sang sản xuất?

**Trả lời:**
```bash
# 1. Sao lưu database sản xuất
sqlcmd -S prod-server -E -Q "BACKUP DATABASE WidgetData TO DISK='C:\Backups\WidgetData_Pre_Migration.bak'"

# 2. Tạo script migration
dotnet ef migrations script --project src/WidgetData.Infrastructure --output migration.sql

# 3. Xem xét script, sau đó áp dụng
sqlcmd -S prod-server -E -i migration.sql

# 4. Triển khai ứng dụng
dotnet publish -c Release -o C:\Deploy\WidgetData

# 5. Cập nhật appsettings.Production.json
# 6. Khởi động lại IIS
iisreset
```

---

## 🛠️ Công cụ Chẩn đoán

### 1. Health Check Endpoint

```bash
curl https://localhost:5001/health
```

Phản hồi mong đợi:
```json
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "redis": "Healthy",
    "hangfire": "Healthy"
  }
}
```

---

### 2. Kiểm tra Kết nối Database

```csharp
public async Task<bool> TestDatabaseConnectionAsync()
{
    try
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";
        var result = await command.ExecuteScalarAsync();
        
        return result != null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Kiểm tra kết nối database thất bại");
        return false;
    }
}
```

---

### 3. Kiểm tra Jobs Đang Chạy

```sql
-- Các job Hangfire đang hoạt động
SELECT 
    Id, 
    StateName, 
    InvocationData,
    CreatedAt
FROM HangFire.Job
WHERE StateName IN ('Enqueued', 'Processing')
ORDER BY CreatedAt DESC;
```

---

### 4. Xem Lỗi Gần Đây

```sql
-- Log ứng dụng (nếu dùng Serilog.Sinks.MSSqlServer)
SELECT TOP 100
    TimeStamp,
    Level,
    Message,
    Exception
FROM Logs
WHERE Level = 'Error'
ORDER BY TimeStamp DESC;
```

---

## 📞 Hỗ trợ

### Trước khi Đặt câu hỏi

1. ✅ Kiểm tra log: `logs/log-{date}.txt`
2. ✅ Kiểm tra health endpoint: `/health`
3. ✅ Xác minh kết nối database
4. ✅ Tìm kiếm trong hướng dẫn xử lý sự cố này
5. ✅ Kiểm tra GitHub issues

### Khi Báo cáo Sự cố

Cung cấp:
- Thông báo lỗi (stack trace đầy đủ)
- Các bước tái hiện lỗi
- Môi trường (OS, phiên bản .NET, phiên bản SQL Server)
- Cấu hình liên quan (ẩn thông tin bí mật!)
- Log (50–100 dòng cuối)

### Kênh Hỗ trợ

- **GitHub Issues**: https://github.com/your-org/widget-data/issues
- **Discussions**: https://github.com/your-org/widget-data/discussions
- **Email**: support@widgetdata.com
- **Slack**: #widget-data-support

---

← [Quay lại INDEX](INDEX.md)
