# Module & Thư viện

## 📊 Phân tích Coverage

Widget Data sử dụng **70% thư viện có sẵn** và chỉ cần **custom 30%** logic nghiệp vụ.

```
┌────────────────────────────────────────────┐
│  🟢 Available Libraries: 70%               │
│  🔵 Custom Development: 30%                │
└────────────────────────────────────────────┘
```

---

## 🟢 Available Libraries (70%)

### 1. Backend Framework (15%)

| Module | Library | Version | Purpose |
|--------|---------|---------|---------|
| **Web API** | ASP.NET Core | 8.0 | REST API framework |
| **ORM** | Entity Framework Core | 8.0 | Database access |
| **Dependency Injection** | Microsoft.Extensions.DI | 8.0 | IoC container |
| **Configuration** | Microsoft.Extensions.Configuration | 8.0 | App settings |
| **Logging** | Serilog | 3.1.1 | Structured logging |

```bash
dotnet add package Microsoft.AspNetCore.App
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.0
dotnet add package Serilog.AspNetCore --version 8.0.0
```

---

### 2. Data Access & Processing (15%)

| Module | Library | Version | Purpose |
|--------|---------|---------|---------|
| **CSV** | CsvHelper | 30.0.1 | Read/Write CSV |
| **Excel** | EPPlus | 7.0.0 | Read/Write Excel |
| **Excel (Alt)** | ClosedXML | 0.102.0 | Excel manipulation |
| **JSON** | System.Text.Json | Built-in | JSON parsing |
| **XML** | System.Xml.Linq | Built-in | XML processing |
| **SQL** | Dapper | 2.1.0 | Micro-ORM (fast queries) |

```bash
dotnet add package CsvHelper --version 30.0.1
dotnet add package EPPlus --version 7.0.0
dotnet add package ClosedXML --version 0.102.0
dotnet add package Dapper --version 2.1.0
```

**Ví dụ CSV:**
```csharp
using CsvHelper;

public async Task<List<ProductDto>> ReadCsvAsync(string path)
{
    using var reader = new StreamReader(path);
    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
    
    var records = csv.GetRecords<ProductDto>().ToList();
    return records;
}
```

**Ví dụ Excel:**
```csharp
using OfficeOpenXml;

public async Task<List<SalesDto>> ReadExcelAsync(string path)
{
    using var package = new ExcelPackage(new FileInfo(path));
    var worksheet = package.Workbook.Worksheets[0];
    
    var sales = new List<SalesDto>();
    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
    {
        sales.Add(new SalesDto
        {
            OrderId = worksheet.Cells[row, 1].GetValue<int>(),
            Amount = worksheet.Cells[row, 2].GetValue<decimal>()
        });
    }
    
    return sales;
}
```

---

### 3. Scheduling & Background Jobs (10%)

| Module | Library | Version | Purpose |
|--------|---------|---------|---------|
| **Worker Service** | Microsoft.NET.Sdk.Worker | 10.0 | BackgroundService host cho cron jobs |
| **Cron Parser** | Cronos | 0.8.4 | Parse & tính `NextRunAt` từ cron expression + timezone |
| **Hangfire (retained)** | Hangfire.Core | 1.8.23 | Background job utilities (InMemory) |

```bash
dotnet add package Cronos --version 0.8.4
```

**Cấu trúc WidgetData.Worker:**
```
src/WidgetData.Worker/
├── Program.cs                      # Host setup (AddInfrastructure + Serilog)
├── Workers/
│   └── SchedulerWorkerService.cs  # BackgroundService chính
├── appsettings.json                # PollingIntervalSeconds config
└── WidgetData.Worker.csproj
```

**SchedulerWorkerService — logic:**
```csharp
// Mỗi PollingIntervalSeconds (mặc định 30s):
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        // 1. Lấy schedule đến hạn
        var due = await scheduleRepo.GetDueAsync(DateTime.UtcNow);
        // 2. Chạy song song, mỗi schedule trong scope riêng
        await Task.WhenAll(due.Select(s => RunScheduleAsync(s.Id, stoppingToken)));
        await Task.Delay(_pollingInterval, stoppingToken);
    }
}

// Sau mỗi lần chạy:
schedule.LastRunAt     = now;
schedule.LastRunStatus = status;                              // Success | Failed
schedule.NextRunAt     = CronUtils.GetNextOccurrence(
    schedule.CronExpression, schedule.Timezone, now);
await scheduleRepo.UpdateAsync(schedule);
```

**CronUtils helper (`Infrastructure/Helpers/CronUtils.cs`):**
```csharp
// Tính lần chạy tiếp theo (timezone-aware)
var next = CronUtils.GetNextOccurrence("0 */6 * * *", "Asia/Ho_Chi_Minh");
// → DateTime UTC của lần chạy tiếp theo sau now

// Kiểm tra cron expression có hợp lệ
bool ok = CronUtils.IsValid("0 */6 * * *"); // true
```

**Ví dụ tạo schedule (NextRunAt tự tính):**
```csharp
// POST /api/schedules
{
  "widgetId": 5,
  "cronExpression": "0 8 * * 1-5",  // 8h sáng, thứ 2–6
  "timezone": "Asia/Ho_Chi_Minh",
  "isEnabled": true,
  "retryOnFailure": true,
  "maxRetries": 3
}
// Response sẽ có NextRunAt = lần 8h sáng ngày làm việc tiếp theo
```

---

### 4. Caching (8%)

| Module | Library | Version | Purpose |
|--------|---------|---------|---------|
| **In-Memory** | Microsoft.Extensions.Caching.Memory | 8.0 | Memory cache |
| **Distributed** | Microsoft.Extensions.Caching.StackExchangeRedis | 8.0 | Redis cache |
| **Redis Client** | StackExchange.Redis | 2.7.0 | Redis operations |

```bash
dotnet add package Microsoft.Extensions.Caching.Memory
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
```

**Ví dụ:**
```csharp
// In-Memory Cache
private readonly IMemoryCache _cache;

public async Task<WidgetData> GetCachedDataAsync(int widgetId)
{
    var cacheKey = $"widget_{widgetId}";
    
    if (!_cache.TryGetValue(cacheKey, out WidgetData data))
    {
        data = await _repository.GetDataAsync(widgetId);
        
        _cache.Set(cacheKey, data, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });
    }
    
    return data;
}
```

---

### 5. Real-Time Communication (5%)

| Module | Library | Version | Purpose |
|--------|---------|---------|---------|
| **SignalR** | Microsoft.AspNetCore.SignalR | 8.0 | Real-time updates |

```bash
dotnet add package Microsoft.AspNetCore.SignalR
```

**Ví dụ:**
```csharp
// Hub
public class WidgetHub : Hub
{
    public async Task BroadcastWidgetUpdate(int widgetId, object data)
    {
        await Clients.Group($"widget_{widgetId}").SendAsync("WidgetUpdated", data);
    }
}

// Client (Blazor)
@inject NavigationManager Navigation

private HubConnection _hubConnection;

protected override async Task OnInitializedAsync()
{
    _hubConnection = new HubConnectionBuilder()
        .WithUrl(Navigation.ToAbsoluteUri("/widgetHub"))
        .Build();
    
    _hubConnection.On<object>("WidgetUpdated", (data) =>
    {
        // Update UI
        StateHasChanged();
    });
    
    await _hubConnection.StartAsync();
}
```

---

### 6. Authentication & Security (7%)

| Module | Library | Version | Purpose |
|--------|---------|---------|---------|
| **Identity** | Microsoft.AspNetCore.Identity | 8.0 | User management |
| **JWT** | System.IdentityModel.Tokens.Jwt | 7.0.0 | JWT tokens |
| **Authentication** | Microsoft.AspNetCore.Authentication.JwtBearer | 8.0 | JWT auth |

```bash
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package System.IdentityModel.Tokens.Jwt
```

---

### 7. Blazor Frontend (10%)

| Module | Library | Version | Purpose |
|--------|---------|---------|---------|
| **UI Framework** | MudBlazor | 6.11.0 | Material Design UI |
| **Charts** | ChartJs.Blazor | 2.0.2 | Chart.js wrapper |
| **Code Editor** | BlazorMonaco | 3.1.0 | Monaco editor |
| **HTTP Client** | System.Net.Http.Json | Built-in | HTTP calls |

```bash
dotnet add package MudBlazor --version 6.11.0
dotnet add package ChartJs.Blazor --version 2.0.2
dotnet add package BlazorMonaco --version 3.1.0
```

**Ví dụ MudBlazor:**
```razor
@page "/widgets"
@using MudBlazor

<MudTable Items="@widgets" Hover="true" Breakpoint="Breakpoint.Sm">
    <HeaderContent>
        <MudTh>Name</MudTh>
        <MudTh>Type</MudTh>
        <MudTh>Status</MudTh>
        <MudTh>Actions</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd DataLabel="Name">@context.Name</MudTd>
        <MudTd DataLabel="Type">@context.WidgetType</MudTd>
        <MudTd DataLabel="Status">
            <MudChip Color="@(context.IsActive ? Color.Success : Color.Default)">
                @(context.IsActive ? "Active" : "Inactive")
            </MudChip>
        </MudTd>
        <MudTd>
            <MudIconButton Icon="@Icons.Material.Filled.Edit" OnClick="@(() => Edit(context))" />
            <MudIconButton Icon="@Icons.Material.Filled.Delete" OnClick="@(() => Delete(context))" />
        </MudTd>
    </RowTemplate>
</MudTable>
```

---

## 🔵 Custom Development (30%)

### 1. Widget Engine (10%)

**Chức năng cần custom:**
- Widget configuration parser (JSON → Object)
- Data transformation pipeline
- Multi-step execution engine
- Branching logic (if-else, switch-case)
- Variable resolution & substitution

```csharp
// Custom: WidgetEngine.cs
public class WidgetEngine
{
    public async Task<WidgetResult> ExecuteAsync(WidgetConfig config)
    {
        var context = new ExecutionContext
        {
            Variables = config.Variables ?? new Dictionary<string, object>()
        };
        
        foreach (var step in config.Steps)
        {
            var result = await ExecuteStepAsync(step, context);
            context.SetVariable(step.OutputVariable, result);
            
            // Handle branching
            if (step.Branches != null)
            {
                var branch = EvaluateBranch(step.Branches, context);
                if (branch != null)
                {
                    await ExecuteBranchAsync(branch, context);
                }
            }
        }
        
        return new WidgetResult
        {
            Data = context.GetVariable("final_output"),
            ExecutedAt = DateTime.UtcNow
        };
    }
}
```

---

### 2. Data Source Connectors (8%)

**Chức năng cần custom:**
- Unified interface cho tất cả data sources
- Connection pooling & retry logic
- Query builder cho dynamic queries
- Schema inference

```csharp
// Custom: IDataSourceConnector.cs
public interface IDataSourceConnector
{
    Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters);
    Task<List<string>> GetTablesAsync();
    Task<DataSchema> GetSchemaAsync(string tableName);
    Task TestConnectionAsync();
}

// Custom: SqlServerConnector.cs
public class SqlServerConnector : IDataSourceConnector
{
    private readonly string _connectionString;
    
    public async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(query, connection);
        
        foreach (var param in parameters)
        {
            command.Parameters.AddWithValue($"@{param.Key}", param.Value);
        }
        
        var dataTable = new DataTable();
        using var adapter = new SqlDataAdapter(command);
        await Task.Run(() => adapter.Fill(dataTable));
        
        return dataTable;
    }
}
```

---

### 3. Widget Builder UI (7%)

**Chức năng cần custom:**
- Drag-and-drop step builder
- Visual query builder
- Real-time preview
- Configuration wizard

```razor
<!-- Custom: WidgetBuilder.razor -->
@page "/builder"

<MudGrid>
    <MudItem xs="3">
        <MudPaper Class="pa-4">
            <MudText Typo="Typo.h6">Steps</MudText>
            <MudList>
                <MudListItem @ondrop="@(() => AddStep("extract"))">
                    <MudIcon Icon="@Icons.Material.Filled.DataArray" /> Extract
                </MudListItem>
                <MudListItem @ondrop="@(() => AddStep("transform"))">
                    <MudIcon Icon="@Icons.Material.Filled.Transform" /> Transform
                </MudListItem>
            </MudList>
        </MudPaper>
    </MudItem>
    
    <MudItem xs="6">
        <MudPaper Class="pa-4">
            <MudText Typo="Typo.h6">Pipeline</MudText>
            @foreach (var step in steps)
            {
                <StepCard Step="@step" OnDelete="@(() => RemoveStep(step))" />
            }
        </MudPaper>
    </MudItem>
    
    <MudItem xs="3">
        <MudPaper Class="pa-4">
            <MudText Typo="Typo.h6">Preview</MudText>
            <DataPreview Data="@previewData" />
        </MudPaper>
    </MudItem>
</MudGrid>
```

---

### 4. Dashboard & Visualization (5%)

**Chức năng cần custom:**
- Custom chart renderers
- Real-time data refresh
- Dashboard layout engine
- Widget resize/reposition

```razor
<!-- Custom: Dashboard.razor -->
@page "/dashboard/{id:int}"

<GridLayout @bind-Layout="layout">
    @foreach (var widget in widgets)
    {
        <GridLayoutItem x="@widget.X" y="@widget.Y" w="@widget.Width" h="@widget.Height">
            <WidgetRenderer Widget="@widget" OnRefresh="@RefreshWidget" />
        </GridLayoutItem>
    }
</GridLayout>

@code {
    private async Task RefreshWidget(Widget widget)
    {
        var data = await _widgetService.GetDataAsync(widget.Id);
        await _hubConnection.InvokeAsync("BroadcastWidgetUpdate", widget.Id, data);
    }
}
```

---

## 📦 Complete Package List

### WidgetData.Infrastructure

```xml
<ItemGroup>
  <!-- Framework -->
  <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.0.7" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.7" />

  <!-- Data Processing -->
  <PackageReference Include="ClosedXML" Version="0.105.0" />

  <!-- Scheduling (cron expression + NextRunAt) -->
  <PackageReference Include="Cronos" Version="0.8.4" />

  <!-- Background job utilities (retained) -->
  <PackageReference Include="Hangfire.Core" Version="1.8.23" />
  <PackageReference Include="Hangfire.InMemory" Version="1.0.0" />
  <PackageReference Include="Hangfire.AspNetCore" Version="1.8.23" />

  <!-- Email / Notifications -->
  <PackageReference Include="MailKit" Version="4.16.0" />
  <PackageReference Include="MimeKit" Version="4.16.0" />
  <PackageReference Include="Telegram.Bot" Version="22.9.6.1" />

  <!-- PDF -->
  <PackageReference Include="QuestPDF" Version="2026.2.4" />

  <!-- SSH -->
  <PackageReference Include="SSH.NET" Version="2025.1.0" />

  <!-- Logging -->
  <PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />
</ItemGroup>
```

### WidgetData.API

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.7" />
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.7" />
  <PackageReference Include="Scalar.AspNetCore" Version="2.14.4" />
  <PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />
  <PackageReference Include="MailKit" Version="4.16.0" />
</ItemGroup>
```

### WidgetData.Worker

```xml
<ItemGroup>
  <!-- Worker SDK: Microsoft.NET.Sdk.Worker -->
  <PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />
  <!-- References: WidgetData.Infrastructure + WidgetData.ServiceDefaults -->
</ItemGroup>
```

### WidgetData.Web (Blazor)

```xml
<ItemGroup>
  <!-- UI -->
  <PackageReference Include="MudBlazor" />
  <!-- Charts, Monaco Editor, ... -->
</ItemGroup>
```

---

## 📊 Effort Estimation

| Category | Available | Custom | Effort (days) |
|----------|-----------|--------|---------------|
| Backend API | 90% | 10% | 5 |
| Data Processing | 80% | 20% | 10 |
| Scheduling | 95% | 5% | 3 |
| Caching | 95% | 5% | 2 |
| Real-time | 90% | 10% | 5 |
| Auth & Security | 85% | 15% | 7 |
| **Widget Engine** | 30% | **70%** | **20** |
| **Data Connectors** | 40% | **60%** | **15** |
| **UI Builder** | 50% | **50%** | **25** |
| Dashboard | 60% | 40% | 12 |
| Testing | 70% | 30% | 10 |
| **TOTAL** | **70%** | **30%** | **~114 days** |

**Team size:** 3 developers  
**Timeline:** ~12-14 weeks (3 months)

---

## 🎯 Development Priority

### Phase 1: Foundation (Weeks 1-4)
1. ✅ Setup ASP.NET Core + EF Core
2. ✅ Authentication & Authorization
3. ✅ Basic CRUD APIs
4. ✅ Database schema

### Phase 2: Core Engine (Weeks 5-8)
1. 🔄 Widget Engine (multi-step execution)
2. 🔄 Data source connectors (SQL, CSV, Excel)
3. 🔄 Scheduling with Hangfire
4. 🔄 Caching layer

### Phase 3: UI & Visualization (Weeks 9-12)
1. ⏳ Blazor dashboard
2. ⏳ Widget builder UI
3. ⏳ Real-time updates (SignalR)
4. ⏳ Charts & visualization

### Phase 4: Polish (Weeks 13-14)
1. ⏳ Testing & QA
2. ⏳ Performance optimization
3. ⏳ Documentation
4. ⏳ Deployment

---

← [Quay lại INDEX](INDEX.md)
