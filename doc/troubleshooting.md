# Troubleshooting & FAQ

## 📋 Common Issues

### 🔴 Database Connection Errors

**Error:** `Cannot open database "WidgetData" requested by the login`

**Causes:**
- Database doesn't exist
- Connection string incorrect
- SQL Server not running

**Solutions:**
```bash
# 1. Verify SQL Server is running
Get-Service MSSQLSERVER

# 2. Test connection
sqlcmd -S localhost -E -Q "SELECT @@VERSION"

# 3. Create database if missing
dotnet ef database update --project src/WidgetData.Infrastructure

# 4. Check connection string
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=WidgetData;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

---

**Error:** `Login failed for user 'IIS APPPOOL\WidgetDataAppPool'`

**Solution:**
```sql
-- Grant access to IIS AppPool user
USE WidgetData;
CREATE USER [IIS APPPOOL\WidgetDataAppPool] FOR LOGIN [IIS APPPOOL\WidgetDataAppPool];
ALTER ROLE db_datareader ADD MEMBER [IIS APPPOOL\WidgetDataAppPool];
ALTER ROLE db_datawriter ADD MEMBER [IIS APPPOOL\WidgetDataAppPool];
```

---

### 🔴 EF Core Migration Issues

**Error:** `Unable to create an object of type 'ApplicationDbContext'`

**Solution:**
```bash
# Specify startup project
dotnet ef migrations add InitialCreate \
  --project src/WidgetData.Infrastructure \
  --startup-project src/WidgetData.Web

dotnet ef database update \
  --project src/WidgetData.Infrastructure \
  --startup-project src/WidgetData.Web
```

---

**Error:** `The migration '20260410_Initial' has already been applied`

**Solution:**
```bash
# Remove last migration
dotnet ef migrations remove --project src/WidgetData.Infrastructure

# Or force reapply
dotnet ef database update 0 --project src/WidgetData.Infrastructure
dotnet ef database update --project src/WidgetData.Infrastructure
```

---

### 🔴 Hangfire Issues

**Error:** `Hangfire dashboard returns 404`

**Solution:**
```csharp
// Ensure middleware is registered in correct order
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

**Error:** `BackgroundJob is not processed`

**Solutions:**
```csharp
// 1. Check Hangfire Server is running
services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 5;
});

// 2. Check job is enqueued
var jobId = BackgroundJob.Enqueue<MyService>(x => x.DoWork());
Console.WriteLine($"Job ID: {jobId}"); // Should not be null

// 3. Check Hangfire logs
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Hangfire", LogEventLevel.Debug)
    .WriteTo.Console()
    .CreateLogger();
```

---

### 🔴 Redis Connection Issues

**Error:** `It was not possible to connect to the redis server(s)`

**Solutions:**
```bash
# 1. Check Redis is running
redis-cli ping
# Expected: PONG

# 2. Check connection string
{
  "Redis": {
    "Configuration": "localhost:6379",
    "InstanceName": "WidgetData:"
  }
}

# 3. Allow remote connections (if needed)
# Edit redis.conf
bind 0.0.0.0
protected-mode no

# Restart Redis
redis-server --service-stop
redis-server --service-start
```

---

### 🔴 Blazor Issues

**Error:** `Blazor app shows blank page`

**Solutions:**
```bash
# 1. Check browser console for errors
# 2. Verify _framework/blazor.webassembly.js is loaded
# 3. Clear browser cache
# 4. Rebuild with clean
dotnet clean
dotnet build
```

---

**Error:** `SignalR connection failed`

**Solution:**
```csharp
// Enable detailed SignalR logging
services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});

// Client-side (Blazor)
_hubConnection = new HubConnectionBuilder()
    .WithUrl(Navigation.ToAbsoluteUri("/widgetHub"))
    .ConfigureLogging(logging =>
    {
        logging.SetMinimumLevel(LogLevel.Debug);
    })
    .Build();
```

---

### 🔴 Performance Issues

**Issue:** Slow widget execution (> 5 seconds)

**Diagnostics:**
```csharp
// Add timing logs
var stopwatch = Stopwatch.StartNew();

_logger.LogInformation("Step 1: Extract started");
var data = await ExtractDataAsync();
_logger.LogInformation("Step 1 completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

stopwatch.Restart();
_logger.LogInformation("Step 2: Transform started");
var transformed = await TransformDataAsync(data);
_logger.LogInformation("Step 2 completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
```

**Solutions:**
```csharp
// 1. Add indexes
CREATE NONCLUSTERED INDEX IX_Orders_Date ON Orders(OrderDate);

// 2. Use pagination
var query = _context.Orders
    .Where(o => o.OrderDate >= startDate)
    .Take(1000); // Limit rows

// 3. Enable caching
var cacheKey = $"widget_{widgetId}";
var data = await _cache.GetOrCreateAsync(cacheKey, async entry =>
{
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
    return await FetchDataAsync();
});

// 4. Use async properly
// ❌ BAD
var result = _service.GetDataAsync().Result; // Blocks!

// ✅ GOOD
var result = await _service.GetDataAsync();
```

---

### 🔴 File Upload Issues

**Error:** `Request body too large`

**Solution:**
```csharp
// Increase max request size
services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100_000_000; // 100 MB
});

// In Controller
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

## ❓ Frequently Asked Questions

### Q: How do I reset admin password?

**A:** Run this SQL script:
```sql
-- Find user
SELECT * FROM AspNetUsers WHERE Email = 'admin@widgetdata.com';

-- Update password (hash for "Admin@123")
UPDATE AspNetUsers
SET PasswordHash = 'AQAAAAIAAYagAAAAEL...' -- Use proper hash
WHERE Email = 'admin@widgetdata.com';
```

Or use CLI tool:
```bash
dotnet run --project src/WidgetData.Web -- reset-password admin@widgetdata.com
```

---

### Q: How to enable debug logging?

**A:** Update `appsettings.Development.json`:
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

### Q: How to clear cache manually?

**A:**
```bash
# Redis
redis-cli FLUSHDB

# In-memory cache (restart app)
iisreset

# Or via API
curl -X POST https://localhost:7001/api/cache/clear -H "Authorization: Bearer YOUR_TOKEN"
```

---

### Q: Widget shows "No data" but query returns rows?

**A:** Check:
```csharp
// 1. Verify query actually returns data
var testResult = await _db.ExecuteQueryAsync("SELECT * FROM Orders");
_logger.LogInformation("Query returned {Count} rows", testResult.Rows.Count);

// 2. Check output variable mapping
{
  "step_id": 1,
  "output": "orders" // ✅ Make sure this matches next step's input
}

// 3. Check data type mapping
// If column is DECIMAL, make sure it's not being truncated
```

---

### Q: How to schedule widget to run every 5 minutes?

**A:**
```json
{
  "widget_id": 123,
  "schedule": {
    "cron": "*/5 * * * *",
    "timezone": "UTC"
  }
}
```

Cron format: `minute hour day month weekday`

Examples:
- Every 5 min: `*/5 * * * *`
- Every hour: `0 * * * *`
- Daily at 2 AM: `0 2 * * *`
- Mondays at 9 AM: `0 9 * * 1`

---

### Q: How to export widget data to Excel?

**A:**
```csharp
[HttpGet("widgets/{id}/export/excel")]
public async Task<IActionResult> ExportExcel(int id)
{
    var data = await _widgetService.GetDataAsync(id);
    
    using var package = new ExcelPackage();
    var worksheet = package.Workbook.Worksheets.Add("Widget Data");
    
    // Load data
    worksheet.Cells["A1"].LoadFromDataTable(data, true);
    
    // Auto-fit columns
    worksheet.Cells.AutoFitColumns();
    
    var bytes = await package.GetAsByteArrayAsync();
    return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
        $"widget_{id}.xlsx");
}
```

---

### Q: How to add a new data source type?

**A:**
```csharp
// 1. Create connector
public class CustomApiConnector : IDataSourceConnector
{
    public async Task<DataTable> ExecuteQueryAsync(string endpoint, Dictionary<string, object> parameters)
    {
        var response = await _httpClient.GetAsync(endpoint);
        var json = await response.Content.ReadAsStringAsync();
        
        // Parse JSON to DataTable
        var data = JsonConvert.DeserializeObject<DataTable>(json);
        return data;
    }
}

// 2. Register
services.AddScoped<IDataSourceConnector, CustomApiConnector>();

// 3. Use in widget
{
  "source": {
    "type": "custom_api",
    "endpoint": "https://api.example.com/data"
  }
}
```

---

### Q: Memory usage keeps increasing

**A:** Common causes:

```csharp
// 1. Not disposing DbContext
// ❌ BAD
public class MyService
{
    private readonly ApplicationDbContext _context; // Singleton lifetime!
}

// ✅ GOOD
public class MyService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    
    public async Task DoWorkAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        // Use context
    }
}

// 2. Caching too much data
// Set expiration
_cache.Set(key, data, new MemoryCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
    SlidingExpiration = TimeSpan.FromMinutes(2)
});

// 3. Not disposing HttpClient
// Use IHttpClientFactory
services.AddHttpClient();
```

---

### Q: How to migrate from development to production?

**A:**
```bash
# 1. Backup production database
sqlcmd -S prod-server -E -Q "BACKUP DATABASE WidgetData TO DISK='C:\Backups\WidgetData_Pre_Migration.bak'"

# 2. Generate migration script
dotnet ef migrations script --project src/WidgetData.Infrastructure --output migration.sql

# 3. Review script, then apply
sqlcmd -S prod-server -E -i migration.sql

# 4. Deploy application
dotnet publish -c Release -o C:\Deploy\WidgetData

# 5. Update appsettings.Production.json
# 6. Restart IIS
iisreset
```

---

## 🛠️ Diagnostic Tools

### 1. Health Check Endpoint

```bash
curl https://localhost:5001/health
```

Expected response:
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

### 2. Database Connection Test

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
        _logger.LogError(ex, "Database connection test failed");
        return false;
    }
}
```

---

### 3. Check Running Jobs

```sql
-- Active Hangfire jobs
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

### 4. View Recent Errors

```sql
-- Application logs (if using Serilog.Sinks.MSSqlServer)
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

## 📞 Getting Help

### Before Asking for Help

1. ✅ Check logs: `logs/log-{date}.txt`
2. ✅ Check health endpoint: `/health`
3. ✅ Verify database connection
4. ✅ Search this troubleshooting guide
5. ✅ Check GitHub issues

### When Reporting Issues

Include:
- Error message (full stack trace)
- Steps to reproduce
- Environment (OS, .NET version, SQL Server version)
- Relevant configuration (redact secrets!)
- Logs (last 50-100 lines)

### Support Channels

- **GitHub Issues**: https://github.com/your-org/widget-data/issues
- **Discussions**: https://github.com/your-org/widget-data/discussions
- **Email**: support@widgetdata.com
- **Slack**: #widget-data-support

---

← [Quay lại INDEX](INDEX.md)
