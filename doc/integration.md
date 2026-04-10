# Integration & Extensibility

## 📋 Tổng quan

Widget Data được thiết kế để **dễ dàng tích hợp** với:

1. **Webhooks** - Event-driven notifications
2. **REST API** - Standard HTTP endpoints
3. **Plugins** - Custom step types
4. **External Systems** - Power BI, Excel, Email
5. **SDK/Libraries** - Client libraries

---

## 🔔 1. Webhooks

### Webhook Configuration

```csharp
public class Webhook
{
    public int Id { get; set; }
    public int WidgetId { get; set; }
    public string Url { get; set; }
    public string[] Events { get; set; } // ["widget.executed", "widget.failed"]
    public string Secret { get; set; } // For signature verification
    public bool IsActive { get; set; }
}
```

### Webhook Events

| Event | Trigger | Payload |
|-------|---------|---------|
| `widget.created` | Widget created | Widget object |
| `widget.updated` | Widget configuration changed | Widget + changes |
| `widget.deleted` | Widget deleted | Widget ID |
| `widget.executed` | Widget execution completed | Widget ID + result |
| `widget.failed` | Widget execution failed | Widget ID + error |
| `schedule.triggered` | Scheduled job started | Schedule ID + timestamp |

### Send Webhook

```csharp
public class WebhookService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookService> _logger;
    
    public async Task TriggerWebhookAsync(string eventType, object payload, Webhook webhook)
    {
        if (!webhook.IsActive || !webhook.Events.Contains(eventType))
        {
            return;
        }
        
        var client = _httpClientFactory.CreateClient();
        
        var webhookPayload = new
        {
            @event = eventType,
            timestamp = DateTime.UtcNow,
            data = payload
        };
        
        var json = JsonSerializer.Serialize(webhookPayload);
        
        // Create signature
        var signature = CreateHmacSignature(json, webhook.Secret);
        
        var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        
        request.Headers.Add("X-Webhook-Signature", signature);
        request.Headers.Add("X-Webhook-Event", eventType);
        
        try
        {
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            _logger.LogInformation("Webhook sent to {Url} for event {Event}", 
                webhook.Url, eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send webhook to {Url}", webhook.Url);
        }
    }
    
    private string CreateHmacSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }
}
```

### Usage Example

```csharp
// In WidgetService after execution
public async Task<WidgetData> ExecuteAsync(int widgetId)
{
    var data = await FetchAndProcessDataAsync(widgetId);
    
    // Trigger webhook
    var webhooks = await _webhookRepository.GetByWidgetIdAsync(widgetId);
    foreach (var webhook in webhooks)
    {
        await _webhookService.TriggerWebhookAsync("widget.executed", new
        {
            widgetId = widgetId,
            rowCount = data.Rows.Count,
            executedAt = DateTime.UtcNow
        }, webhook);
    }
    
    return data;
}
```

### Verify Webhook Signature (Receiver Side)

```csharp
// In your webhook receiver API
[HttpPost("webhook")]
public async Task<IActionResult> ReceiveWebhook([FromBody] object payload)
{
    var signature = Request.Headers["X-Webhook-Signature"].FirstOrDefault();
    var eventType = Request.Headers["X-Webhook-Event"].FirstOrDefault();
    
    // Verify signature
    var body = await new StreamReader(Request.Body).ReadToEndAsync();
    var expectedSignature = CreateHmacSignature(body, "your-secret");
    
    if (signature != expectedSignature)
    {
        return Unauthorized("Invalid signature");
    }
    
    // Process event
    switch (eventType)
    {
        case "widget.executed":
            await HandleWidgetExecutedAsync(payload);
            break;
        case "widget.failed":
            await HandleWidgetFailedAsync(payload);
            break;
    }
    
    return Ok();
}
```

---

## 🔌 2. Plugin System

### IStepExecutor Interface

```csharp
public interface IStepExecutor
{
    string StepType { get; } // "extract", "transform", "custom_ml"
    Task<StepResult> ExecuteAsync(StepConfig config, ExecutionContext context);
}
```

### Custom Plugin Example

```csharp
// Custom ML prediction step
public class MachineLearningStepExecutor : IStepExecutor
{
    public string StepType => "ml_predict";
    
    private readonly IMLService _mlService;
    
    public MachineLearningStepExecutor(IMLService mlService)
    {
        _mlService = mlService;
    }
    
    public async Task<StepResult> ExecuteAsync(StepConfig config, ExecutionContext context)
    {
        // Get input data from context
        var inputData = context.GetVariable(config.InputVariable) as DataTable;
        
        // Get ML model path
        var modelPath = config.GetParameter<string>("model_path");
        var inputColumns = config.GetParameter<string[]>("input_columns");
        
        // Make predictions
        var predictions = await _mlService.PredictAsync(modelPath, inputData, inputColumns);
        
        // Return result
        return new StepResult
        {
            Success = true,
            Data = predictions,
            RowsProcessed = predictions.Rows.Count
        };
    }
}
```

### Register Plugin

```csharp
// Program.cs
public void ConfigureServices(IServiceCollection services)
{
    // Register built-in executors
    services.AddScoped<IStepExecutor, ExtractStepExecutor>();
    services.AddScoped<IStepExecutor, TransformStepExecutor>();
    services.AddScoped<IStepExecutor, AggregateStepExecutor>();
    
    // Register custom plugin
    services.AddScoped<IStepExecutor, MachineLearningStepExecutor>();
}
```

### Use Custom Step in Widget

```json
{
  "widget_name": "SalesPrediction",
  "steps": [
    {
      "step_id": 1,
      "step_type": "extract",
      "source": { "type": "database", "query": "SELECT * FROM sales_history" },
      "output": "historical_sales"
    },
    {
      "step_id": 2,
      "step_type": "ml_predict",
      "config": {
        "model_path": "/models/sales_forecast.zip",
        "input_columns": ["month", "region", "product"]
      },
      "input": "historical_sales",
      "output": "predictions"
    }
  ]
}
```

---

## 📊 3. Power BI Integration

### Export to Power BI Dataset

```csharp
public class PowerBIService
{
    private readonly HttpClient _client;
    
    public async Task PublishDatasetAsync(string datasetName, DataTable data)
    {
        var token = await GetAccessTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
        
        // Create dataset
        var dataset = new
        {
            name = datasetName,
            tables = new[]
            {
                new
                {
                    name = "WidgetData",
                    columns = data.Columns.Cast<DataColumn>().Select(c => new
                    {
                        name = c.ColumnName,
                        dataType = MapToEdmType(c.DataType)
                    })
                }
            }
        };
        
        var response = await _client.PostAsJsonAsync(
            "https://api.powerbi.com/v1.0/myorg/datasets",
            dataset
        );
        
        var createdDataset = await response.Content.ReadFromJsonAsync<PowerBIDataset>();
        
        // Push rows
        var rows = data.AsEnumerable().Select(row => 
            data.Columns.Cast<DataColumn>().ToDictionary(
                c => c.ColumnName,
                c => row[c]
            )
        );
        
        await _client.PostAsJsonAsync(
            $"https://api.powerbi.com/v1.0/myorg/datasets/{createdDataset.Id}/tables/WidgetData/rows",
            new { rows }
        );
    }
    
    private string MapToEdmType(Type type)
    {
        return type switch
        {
            Type t when t == typeof(int) => "Int64",
            Type t when t == typeof(decimal) => "Double",
            Type t when t == typeof(DateTime) => "DateTime",
            Type t when t == typeof(bool) => "Boolean",
            _ => "String"
        };
    }
}
```

---

## 📧 4. Email Integration

### Send Widget Results via Email

```csharp
public class EmailService
{
    private readonly SmtpClient _smtpClient;
    
    public async Task SendWidgetResultAsync(Widget widget, WidgetData data, string[] recipients)
    {
        var message = new MailMessage
        {
            From = new MailAddress("noreply@widgetdata.com"),
            Subject = $"Widget Result: {widget.Name}",
            IsBodyHtml = true
        };
        
        foreach (var recipient in recipients)
        {
            message.To.Add(recipient);
        }
        
        // Generate HTML table
        var htmlTable = GenerateHtmlTable(data);
        
        message.Body = $@"
            <h2>{widget.Name}</h2>
            <p>Executed at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}</p>
            <p>Rows: {data.Rows.Count}</p>
            <hr/>
            {htmlTable}
        ";
        
        // Attach CSV
        var csv = GenerateCsv(data);
        var attachment = new Attachment(
            new MemoryStream(Encoding.UTF8.GetBytes(csv)),
            $"{widget.Name}_{DateTime.UtcNow:yyyyMMdd}.csv",
            "text/csv"
        );
        message.Attachments.Add(attachment);
        
        await _smtpClient.SendMailAsync(message);
    }
    
    private string GenerateHtmlTable(WidgetData data)
    {
        var sb = new StringBuilder();
        sb.Append("<table border='1' cellpadding='5' cellspacing='0'>");
        
        // Header
        sb.Append("<thead><tr>");
        foreach (DataColumn col in data.Rows[0].Table.Columns)
        {
            sb.Append($"<th>{col.ColumnName}</th>");
        }
        sb.Append("</tr></thead>");
        
        // Rows (limit to first 100)
        sb.Append("<tbody>");
        foreach (DataRow row in data.Rows.Cast<DataRow>().Take(100))
        {
            sb.Append("<tr>");
            foreach (var item in row.ItemArray)
            {
                sb.Append($"<td>{item}</td>");
            }
            sb.Append("</tr>");
        }
        sb.Append("</tbody></table>");
        
        return sb.ToString();
    }
}
```

### Schedule Email Reports

```csharp
// Configure in Widget
{
  "widget_id": 123,
  "schedule": {
    "cron": "0 8 * * MON",
    "actions": [
      {
        "type": "email",
        "recipients": ["manager@company.com", "team@company.com"],
        "subject": "Weekly Sales Report",
        "include_attachment": true
      }
    ]
  }
}
```

---

## 🔗 5. REST API for External Systems

### API Endpoints

```csharp
[ApiController]
[Route("api/integration")]
public class IntegrationController : ControllerBase
{
    // Execute widget and return results
    [HttpPost("widgets/{id}/execute")]
    public async Task<IActionResult> ExecuteWidget(int id, [FromBody] Dictionary<string, object> parameters)
    {
        var result = await _widgetService.ExecuteAsync(id, parameters);
        return Ok(result);
    }
    
    // Get widget results in various formats
    [HttpGet("widgets/{id}/export")]
    public async Task<IActionResult> ExportWidget(int id, [FromQuery] string format = "json")
    {
        var data = await _widgetService.GetDataAsync(id);
        
        return format.ToLower() switch
        {
            "csv" => File(GenerateCsv(data), "text/csv", $"widget_{id}.csv"),
            "excel" => File(GenerateExcel(data), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"widget_{id}.xlsx"),
            "json" => Ok(data),
            _ => BadRequest("Unsupported format")
        };
    }
    
    // OData endpoint for flexible querying
    [HttpGet("odata/widgets")]
    [EnableQuery]
    public IQueryable<Widget> GetWidgets()
    {
        return _context.Widgets.AsQueryable();
    }
}
```

### Client SDK Example (C#)

```csharp
public class WidgetDataClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    
    public WidgetDataClient(string baseUrl, string apiKey)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _apiKey = apiKey;
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    }
    
    public async Task<WidgetData> ExecuteWidgetAsync(int widgetId, Dictionary<string, object> parameters = null)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/integration/widgets/{widgetId}/execute",
            parameters ?? new Dictionary<string, object>()
        );
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WidgetData>();
    }
    
    public async Task<byte[]> ExportWidgetAsync(int widgetId, string format = "csv")
    {
        var response = await _httpClient.GetAsync($"/api/integration/widgets/{widgetId}/export?format={format}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }
}

// Usage
var client = new WidgetDataClient("https://api.widgetdata.com", "your-api-key");
var data = await client.ExecuteWidgetAsync(123);
var csvBytes = await client.ExportWidgetAsync(123, "csv");
```

---

## 🐍 6. Python SDK

```python
# widgetdata_client.py
import requests
import pandas as pd
from io import StringIO

class WidgetDataClient:
    def __init__(self, base_url, api_key):
        self.base_url = base_url
        self.headers = {"X-API-Key": api_key}
    
    def execute_widget(self, widget_id, parameters=None):
        url = f"{self.base_url}/api/integration/widgets/{widget_id}/execute"
        response = requests.post(url, json=parameters or {}, headers=self.headers)
        response.raise_for_status()
        return response.json()
    
    def export_widget_csv(self, widget_id):
        url = f"{self.base_url}/api/integration/widgets/{widget_id}/export?format=csv"
        response = requests.get(url, headers=self.headers)
        response.raise_for_status()
        return pd.read_csv(StringIO(response.text))
    
    def get_widgets(self, filter_query=None):
        url = f"{self.base_url}/api/integration/odata/widgets"
        if filter_query:
            url += f"?$filter={filter_query}"
        response = requests.get(url, headers=self.headers)
        response.raise_for_status()
        return response.json()

# Usage
client = WidgetDataClient("https://api.widgetdata.com", "your-api-key")
data = client.execute_widget(123, {"start_date": "2026-01-01"})
df = client.export_widget_csv(123)
print(df.head())
```

---

## 📱 7. Mobile App Integration

### Expose Mobile-Friendly API

```csharp
[ApiController]
[Route("api/mobile")]
public class MobileApiController : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var widgets = await _widgetService.GetUserWidgetsAsync(userId);
        
        return Ok(new
        {
            widgets = widgets.Select(w => new
            {
                w.Id,
                w.Name,
                w.WidgetType,
                lastExecuted = w.LastExecutedAt,
                thumbnail = $"/api/widgets/{w.Id}/thumbnail"
            })
        });
    }
    
    [HttpGet("widgets/{id}/summary")]
    public async Task<IActionResult> GetWidgetSummary(int id)
    {
        var data = await _widgetService.GetDataAsync(id);
        
        // Return summary for mobile (not full data)
        return Ok(new
        {
            rowCount = data.Rows.Count,
            lastUpdated = DateTime.UtcNow,
            preview = data.Rows.Take(10) // First 10 rows only
        });
    }
}
```

---

## ✅ Integration Checklist

### Webhooks
- [ ] Webhook endpoints configured
- [ ] HMAC signature verification implemented
- [ ] Retry logic for failed webhooks
- [ ] Webhook logs for debugging

### API
- [ ] REST API documented (Swagger/OpenAPI)
- [ ] API key authentication
- [ ] Rate limiting configured
- [ ] CORS policy set

### Plugins
- [ ] Plugin interface documented
- [ ] Sample plugin provided
- [ ] Plugin registration tested

### External Systems
- [ ] Power BI integration tested
- [ ] Email templates configured
- [ ] Export formats working (CSV, Excel, JSON)

---

← [Quay lại INDEX](INDEX.md)
