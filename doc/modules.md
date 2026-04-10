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
| **Job Scheduler** | Hangfire.Core | 1.8.6 | Background jobs |
| **SQL Storage** | Hangfire.SqlServer | 1.8.6 | Job persistence |
| **Cron Parser** | Cronos | 0.8.3 | Parse cron expressions |

```bash
dotnet add package Hangfire.Core --version 1.8.6
dotnet add package Hangfire.SqlServer --version 1.8.6
dotnet add package Cronos --version 0.8.3
```

**Ví dụ:**
```csharp
// Configure Hangfire
services.AddHangfire(config => 
    config.UseSqlServerStorage(connectionString));
services.AddHangfireServer();

// Schedule recurring job
RecurringJob.AddOrUpdate<WidgetRefreshService>(
    "refresh-sales-widget",
    service => service.RefreshAsync(123),
    "*/5 * * * *" // Every 5 minutes
);
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

### Backend

```xml
<ItemGroup>
  <!-- Framework -->
  <PackageReference Include="Microsoft.AspNetCore.App" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
  
  <!-- Data Processing -->
  <PackageReference Include="CsvHelper" Version="30.0.1" />
  <PackageReference Include="EPPlus" Version="7.0.0" />
  <PackageReference Include="ClosedXML" Version="0.102.0" />
  <PackageReference Include="Dapper" Version="2.1.0" />
  
  <!-- Scheduling -->
  <PackageReference Include="Hangfire.Core" Version="1.8.6" />
  <PackageReference Include="Hangfire.SqlServer" Version="1.8.6" />
  <PackageReference Include="Cronos" Version="0.8.3" />
  
  <!-- Caching -->
  <PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.0" />
  <PackageReference Include="StackExchange.Redis" Version="2.7.0" />
  
  <!-- Auth & Security -->
  <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.0" />
  <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.0.0" />
  
  <!-- Logging -->
  <PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
  <PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
  <PackageReference Include="Serilog.Sinks.Console" Version="5.0.0" />
  
  <!-- Real-time -->
  <PackageReference Include="Microsoft.AspNetCore.SignalR" Version="8.0.0" />
  
  <!-- Utilities -->
  <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.0" />
  <PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
  <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
</ItemGroup>
```

### Frontend (Blazor)

```xml
<ItemGroup>
  <!-- Blazor -->
  <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="8.0.0" />
  
  <!-- UI Components -->
  <PackageReference Include="MudBlazor" Version="6.11.0" />
  <PackageReference Include="ChartJs.Blazor" Version="2.0.2" />
  <PackageReference Include="BlazorMonaco" Version="3.1.0" />
  
  <!-- HTTP -->
  <PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
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
