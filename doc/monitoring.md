# Giám sát & Ghi log

## 📋 Tổng quan

Widget Data sử dụng monitoring & logging stack:

- **Serilog** - Structured logging
- **Application Insights** - Azure telemetry (Production)
- **Seq** - Log aggregation (Development/On-prem)
- **Prometheus + Grafana** - Metrics & dashboards (Optional)
- **Health Checks** - Endpoint monitoring

---

## 📝 1. Ghi log Có cấu trúc với Serilog

### Cấu hình

```csharp
// Program.cs
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Application", "WidgetData")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq("http://localhost:5341") // Optional
    .CreateLogger();

builder.Host.UseSerilog();
```

### Cấu hình appsettings.json

```json
{
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File", "Serilog.Sinks.Seq" ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://localhost:5341"
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ]
  }
}
```

### Sử dụng trong Code

```csharp
public class WidgetService
{
    private readonly ILogger<WidgetService> _logger;
    
    public WidgetService(ILogger<WidgetService> logger)
    {
        _logger = logger;
    }
    
    public async Task<Widget> CreateAsync(WidgetDto dto)
    {
        _logger.LogInformation("Creating widget {WidgetName} of type {WidgetType}", 
            dto.Name, dto.WidgetType);
        
        try
        {
            var widget = new Widget
            {
                Name = dto.Name,
                WidgetType = dto.WidgetType,
                CreatedAt = DateTime.UtcNow
            };
            
            await _repository.AddAsync(widget);
            
            _logger.LogInformation("Widget {WidgetId} created successfully", widget.Id);
            
            return widget;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create widget {WidgetName}", dto.Name);
            throw;
        }
    }
    
    public async Task<WidgetData> GetDataAsync(int widgetId)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["WidgetId"] = widgetId,
            ["Operation"] = "GetData"
        }))
        {
            var stopwatch = Stopwatch.StartNew();
            
            _logger.LogDebug("Fetching data for widget {WidgetId}", widgetId);
            
            var data = await _dataService.FetchAsync(widgetId);
            
            _logger.LogInformation("Retrieved {RowCount} rows in {ElapsedMs}ms", 
                data.Rows.Count, stopwatch.ElapsedMilliseconds);
            
            return data;
        }
    }
}
```

### Các mức Log

```csharp
// Trace: Rất chi tiết, dùng để debug
_logger.LogTrace("Processing step {StepId} with config {Config}", stepId, config);

// Debug: Thông tin chẩn đoán
_logger.LogDebug("Cache hit for key {CacheKey}", key);

// Information: Luồng bình thường, các mốc quan trọng
_logger.LogInformation("Widget {WidgetId} executed successfully", widgetId);

// Warning: Bất thường nhưng đã được xử lý
_logger.LogWarning("Cache miss for key {CacheKey}, fetching from database", key);

// Error: Lỗi và ngoại lệ
_logger.LogError(ex, "Failed to execute widget {WidgetId}", widgetId);

// Critical: Lỗi nghiêm trọng
_logger.LogCritical("Database connection lost!");
```

---

## 📊 2. Application Insights (Azure)

### Cài đặt

```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});

// Or from appsettings.json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=your-key;IngestionEndpoint=https://..."
  }
}
```

### Theo dõi Sự kiện tùy chỉnh

```csharp
public class WidgetService
{
    private readonly TelemetryClient _telemetry;
    
    public async Task<WidgetData> ExecuteAsync(int widgetId)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var data = await FetchDataAsync(widgetId);
            
            // Track custom metrics
            _telemetry.TrackMetric("WidgetExecution.Duration", stopwatch.ElapsedMilliseconds);
            _telemetry.TrackMetric("WidgetExecution.RowCount", data.Rows.Count);
            
            // Track custom event
            _telemetry.TrackEvent("WidgetExecuted", new Dictionary<string, string>
            {
                ["WidgetId"] = widgetId.ToString(),
                ["RowCount"] = data.Rows.Count.ToString()
            });
            
            return data;
        }
        catch (Exception ex)
        {
            _telemetry.TrackException(ex, new Dictionary<string, string>
            {
                ["WidgetId"] = widgetId.ToString(),
                ["Operation"] = "Execute"
            });
            
            throw;
        }
    }
}
```

### Theo dõi Dependencies

```csharp
public async Task<string> CallExternalApiAsync(string url)
{
    var dependencyTelemetry = new DependencyTelemetry
    {
        Name = "External API Call",
        Type = "HTTP",
        Target = url,
        Timestamp = DateTimeOffset.UtcNow
    };
    
    var stopwatch = Stopwatch.StartNew();
    
    try
    {
        var response = await _httpClient.GetStringAsync(url);
        
        dependencyTelemetry.Success = true;
        dependencyTelemetry.Duration = stopwatch.Elapsed;
        
        return response;
    }
    catch (Exception ex)
    {
        dependencyTelemetry.Success = false;
        dependencyTelemetry.Duration = stopwatch.Elapsed;
        
        _telemetry.TrackException(ex);
        
        throw;
    }
    finally
    {
        _telemetry.TrackDependency(dependencyTelemetry);
    }
}
```

### Khởi tạo Telemetry tùy chỉnh

```csharp
public class CustomTelemetryInitializer : ITelemetryInitializer
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public void Initialize(ITelemetry telemetry)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        if (httpContext != null)
        {
            var userId = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                telemetry.Context.User.AuthenticatedUserId = userId;
            }
            
            telemetry.Context.GlobalProperties["UserAgent"] = 
                httpContext.Request.Headers["User-Agent"].ToString();
        }
    }
}

// Đăng ký
builder.Services.AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();
```

---

## 🔍 3. Seq (Máy chủ Log)

### Cài đặt Seq

```bash
# Docker
docker run -d \
  --name seq \
  -e ACCEPT_EULA=Y \
  -p 5341:80 \
  -v C:/seq-data:/data \
  datalust/seq:latest
```

### Truy cập giao diện Seq

http://localhost:5341

### Ví dụ truy vấn

```sql
-- Tìm lỗi trong 1 giờ gần nhất
select *
from stream
where @Level = 'Error'
  and @Timestamp > Now() - 1h

-- Đếm widget được tạo mỗi giờ
select count(*) as WidgetCount
from stream
where @MessageTemplate = 'Widget {WidgetId} created successfully'
group by time(1h)

-- Tìm query chậm
select *
from stream
where @MessageTemplate like '%ElapsedMs%'
  and ElapsedMs > 1000
```

---

## ❤️ 4. Kiểm tra Sức khỏe

### Cấu hình Health Checks

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        name: "database",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "db", "sql" })
    .AddRedis(
        redisConnectionString: builder.Configuration["Redis:Configuration"],
        name: "redis",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "cache", "redis" })
    .AddHangfire(
        options => { },
        name: "hangfire",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "scheduler" })
    .AddCheck<DiskSpaceHealthCheck>("disk_space")
    .AddCheck<ExternalApiHealthCheck>("external_api");

// Map endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Only basic checks
});
```

### Health Check tùy chỉnh

```csharp
public class DiskSpaceHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.Name == "C:\\");
        
        if (drive == null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Drive not found"));
        }
        
        var freeSpaceGB = drive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0;
        
        if (freeSpaceGB < 1)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Low disk space: {freeSpaceGB:F2} GB"));
        }
        
        if (freeSpaceGB < 5)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Disk space warning: {freeSpaceGB:F2} GB"));
        }
        
        return Task.FromResult(HealthCheckResult.Healthy(
            $"Disk space OK: {freeSpaceGB:F2} GB"));
    }
}
```

### Giao diện Health Check (Tùy chọn)

```bash
dotnet add package AspNetCore.HealthChecks.UI
dotnet add package AspNetCore.HealthChecks.UI.InMemory.Storage
```

```csharp
// Program.cs
builder.Services.AddHealthChecksUI(setup =>
{
    setup.AddHealthCheckEndpoint("Widget Data API", "/health");
    setup.SetEvaluationTimeInSeconds(30);
})
.AddInMemoryStorage();

app.MapHealthChecksUI();
```

Access UI: `https://localhost:5001/healthchecks-ui`

---

## 📈 5. Prometheus & Grafana (Tùy chọn)

### Cài đặt Prometheus Metrics

```bash
dotnet add package prometheus-net.AspNetCore
```

```csharp
// Program.cs
using Prometheus;

// Thêm metrics endpoint
app.UseMetricServer(); // /metrics
app.UseHttpMetrics();  // Theo dõi HTTP metrics
```

### Metrics tùy chỉnh

```csharp
public class MetricsService
{
    private static readonly Counter _widgetExecutions = 
        Metrics.CreateCounter("widget_executions_total", "Total widget executions");
    
    private static readonly Histogram _executionDuration = 
        Metrics.CreateHistogram("widget_execution_duration_seconds", "Widget execution duration");
    
    private static readonly Gauge _activeWidgets = 
        Metrics.CreateGauge("active_widgets", "Number of active widgets");
    
    public async Task<WidgetData> ExecuteWidgetAsync(int widgetId)
    {
        _widgetExecutions.Inc();
        
        using (_executionDuration.NewTimer())
        {
            return await _widgetService.ExecuteAsync(widgetId);
        }
    }
    
    public void UpdateActiveWidgets(int count)
    {
        _activeWidgets.Set(count);
    }
}
```

### Cấu hình Prometheus

```yaml
# prometheus.yml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'widgetdata'
    static_configs:
      - targets: ['localhost:7001']
```

### Chạy Prometheus

```bash
docker run -d \
  --name prometheus \
  -p 9090:9090 \
  -v C:/prometheus.yml:/etc/prometheus/prometheus.yml \
  prom/prometheus
```

### Bảng điều khiển Grafana

```bash
docker run -d \
  --name grafana \
  -p 3000:3000 \
  grafana/grafana
```

Access: http://localhost:3000 (admin/admin)

---

## 🚨 6. Theo dõi Lỗi

### Xử lý Ngoại lệ Toàn cục

```csharp
// Middleware
public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            ValidationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
        
        var response = new
        {
            error = exception.Message,
            statusCode = context.Response.StatusCode,
            timestamp = DateTime.UtcNow
        };
        
        return context.Response.WriteAsJsonAsync(response);
    }
}

// Đăng ký
app.UseMiddleware<ExceptionHandlerMiddleware>();
```

### ASP.NET Core Exception Handler

```csharp
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionHandlerFeature?.Error;
        
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(exception, "Unhandled exception");
        
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "An error occurred",
            timestamp = DateTime.UtcNow
        });
    });
});
```

---

## 📊 7. Bảng điều khiển Giám sát

### Ví dụ Truy vấn Grafana

**Tốc độ yêu cầu:**
```promql
rate(http_requests_received_total[5m])
```

**Tỷ lệ lỗi:**
```promql
rate(http_requests_received_total{code=~"5.."}[5m])
```

**Thời gian phản hồi (p95):**
```promql
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))
```

**Widget đang hoạt động:**
```promql
active_widgets
```

---

## 🔔 8. Cảnh báo

### Cảnh báo Email (Seq)

```json
{
  "Name": "Error Rate Alert",
  "Filters": [
    {
      "Property": "@Level",
      "Operator": "Equal",
      "Value": "Error"
    }
  ],
  "Actions": [
    {
      "Type": "Email",
      "To": ["admin@widgetdata.com"],
      "Subject": "High Error Rate Detected",
      "Body": "Error rate exceeded threshold"
    }
  ],
  "Threshold": {
    "Count": 10,
    "Window": "5m"
  }
}
```

### Thông báo Slack

```csharp
public class SlackNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly string _webhookUrl;
    
    public async Task SendAlertAsync(string message, string severity)
    {
        var payload = new
        {
            text = message,
            attachments = new[]
            {
                new
                {
                    color = severity == "critical" ? "danger" : "warning",
                    fields = new[]
                    {
                        new { title = "Severity", value = severity, @short = true },
                        new { title = "Timestamp", value = DateTime.UtcNow.ToString("o"), @short = true }
                    }
                }
            }
        };
        
        await _httpClient.PostAsJsonAsync(_webhookUrl, payload);
    }
}
```

---

## 📋 Danh sách kiểm tra Monitoring

### Phát triển
- [ ] Serilog đã cấu hình với console & file sinks
- [ ] Structured logging trong tất cả services
- [ ] Log levels phù hợp
- [ ] Ghi log exception kèm context
- [ ] Seq đang chạy cục bộ

### Sản xuất
- [ ] Application Insights đã bật
- [ ] Health checks đã cấu hình
- [ ] Prometheus metrics đã expose
- [ ] Grafana dashboards đã tạo
- [ ] Cảnh báo đã cấu hình (email/Slack)
- [ ] Chính sách lưu trữ log đã thiết lập
- [ ] Performance counters được theo dõi

---

← [Quay lại INDEX](INDEX.md)
