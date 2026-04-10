# Kiến trúc & Thiết kế Hệ thống

## Kiến trúc tổng thể

```
┌─────────────────────────────────────────────────────────────────┐
│                        BLAZOR FRONTEND                          │
│  Dashboard | Widget Builder | Live Monitor | History | Settings │
└────────────────────────────┬────────────────────────────────────┘
                             │ SignalR (Real-time)
                             │ HTTPS REST API
┌────────────────────────────┴────────────────────────────────────┐
│                    ASP.NET CORE WEB API                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐         │
│  │ Widget API   │  │  Auth API    │  │  Source API  │         │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘         │
│         │                  │                  │                  │
│  ┌──────┴──────────────────┴──────────────────┴───────┐         │
│  │           Application Services Layer                │         │
│  │  WidgetService | ScheduleService | CacheService    │         │
│  └──────────────────────┬──────────────────────────────┘         │
└─────────────────────────┼────────────────────────────────────────┘
                          │
┌─────────────────────────┴────────────────────────────────────────┐
│                   INFRASTRUCTURE LAYER                           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │ EF Core      │  │   Hangfire   │  │    Redis     │          │
│  │ Repository   │  │   Scheduler  │  │    Cache     │          │
│  └──────┬───────┘  └──────┬───────┘  └──────────────┘          │
└─────────┼──────────────────┼──────────────────────────────────────┘
          │                  │
┌─────────┴──────────────────┴──────────────────────────────────────┐
│                      DATA SOURCES                                 │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐           │
│  │SQL Server│ │PostgreSQL│ │   Files  │ │   APIs   │           │
│  │          │ │  MySQL   │ │CSV/JSON  │ │ REST/etc │           │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘           │
└───────────────────────────────────────────────────────────────────┘
```

## Luồng dữ liệu (Data Flow)

### 1. Widget Execution Flow
```
User Request → API Controller → WidgetService
                                      ↓
                               Check Cache ────→ Cache Hit → Return Data
                                      ↓ Cache Miss
                               DataSourceAdapter
                                      ↓
                          Execute Query/Read File
                                      ↓
                             Transform Data
                                      ↓
                          Store in Cache + DB
                                      ↓
                    SignalR Push (if subscribed)
                                      ↓
                            Return to Client
```

### 2. Scheduled Widget Flow
```
Hangfire Scheduler (Cron/Interval)
         ↓
  Trigger WidgetService.Execute()
         ↓
  Execute Widget → Cache Result
         ↓
  Log to History
         ↓
  SignalR Broadcast (to live subscribers)
         ↓
  Send Alerts (if threshold met)
```

## Layered Architecture

```
┌───────────────────────────────────────────────────────────┐
│ Presentation Layer (Blazor)                               │
│ - Pages, Components, ViewModels                           │
└───────────────────────────┬───────────────────────────────┘
                            │
┌───────────────────────────┴───────────────────────────────┐
│ API Layer (ASP.NET Core Controllers)                      │
│ - Request/Response handling, Validation, Auth             │
└───────────────────────────┬───────────────────────────────┘
                            │
┌───────────────────────────┴───────────────────────────────┐
│ Business Logic Layer (Services)                           │
│ - WidgetService, ScheduleService, CacheService            │
│ - Business rules, Orchestration                           │
└───────────────────────────┬───────────────────────────────┘
                            │
┌───────────────────────────┴───────────────────────────────┐
│ Data Access Layer (Repositories)                          │
│ - Entity Framework Core, Dapper                           │
│ - Data Source Adapters                                    │
└───────────────────────────┬───────────────────────────────┘
                            │
┌───────────────────────────┴───────────────────────────────┐
│ Database & External Sources                               │
│ - SQL Server, Files, APIs                                 │
└───────────────────────────────────────────────────────────┘
```

## Component Design Patterns

### 1. Repository Pattern
```csharp
public interface IWidgetRepository {
    Task<Widget> GetByIdAsync(int id);
    Task<IEnumerable<Widget>> GetAllAsync();
    Task<Widget> CreateAsync(Widget widget);
    Task UpdateAsync(Widget widget);
    Task DeleteAsync(int id);
}
```

### 2. Strategy Pattern (Data Source Adapters)
```csharp
public interface IDataSourceAdapter {
    Task<DataResult> ExecuteAsync(WidgetConfiguration config);
    bool CanHandle(DataSourceType type);
}

// Implementations:
- SqlServerAdapter
- PostgreSqlAdapter
- CsvFileAdapter
- JsonFileAdapter
- RestApiAdapter
```

### 3. Factory Pattern (Widget Execution)
```csharp
public interface IWidgetExecutorFactory {
    IWidgetExecutor CreateExecutor(DataSourceType type);
}
```

## Technology Stack

### Backend (.NET Core)
- **Framework**: ASP.NET Core 8.0 / 9.0
- **API**: ASP.NET Core Web API (REST)
- **ORM**: Entity Framework Core
- **Database**: SQL Server (primary), PostgreSQL, MySQL, SQLite
- **Job Scheduler**: Hangfire / Quartz.NET
- **Cache**: Redis (IDistributedCache), In-Memory Cache (IMemoryCache)
- **File Processing**: EPPlus/ClosedXML (Excel), CsvHelper (CSV)
- **Real-time**: SignalR
- **Authentication**: ASP.NET Core Identity / JWT

### Frontend (Blazor)
- **Framework**: Blazor Server / Blazor WebAssembly (.NET 8/9)
- **UI Components**: MudBlazor / Radzen Blazor / Ant Design Blazor
- **Charts**: ChartJs.Blazor / ApexCharts.Blazor / Plotly.Blazor
- **Real-time**: SignalR (native integration)
- **Code Editor**: BlazorMonaco (SQL/JSON editing)
- **Data Grid**: MudBlazor DataGrid / Radzen DataGrid

### Storage & Infrastructure
- **Configuration**: appsettings.json / Azure App Configuration
- **Cache Storage**: Redis / SQL Server / File system
- **Logging**: Serilog → File / SQL Server / Seq
- **Deployment**: IIS / Docker / Azure App Service

## Packages chính (.NET)

```
# Backend
Microsoft.EntityFrameworkCore.SqlServer
Microsoft.AspNetCore.SignalR
Hangfire.AspNetCore
StackExchange.Redis
EPPlus / ClosedXML
CsvHelper
Serilog.AspNetCore
Dapper

# Frontend (Blazor)
Microsoft.AspNetCore.Components.WebAssembly (hoặc Blazor Server)
MudBlazor / Radzen.Blazor
ChartJs.Blazor / ApexCharts.Blazor
BlazorMonaco
```

## Lợi thế của stack Full .NET (Backend + Blazor)

✅ **Cùng ngôn ngữ C#**: Share code giữa backend/frontend (models, validation, logic)  
✅ **Type safety**: Strongly typed từ đầu đến cuối  
✅ **SignalR native**: Real-time integration hoàn hảo cho live data  
✅ **Performance**: Blazor Server rất nhanh, Blazor WASM chạy gần native speed  
✅ **Productivity cao**: Không cần context switching giữa C# và JavaScript  
✅ **Debugging tốt**: F5 debug cả backend lẫn frontend trong Visual Studio  
✅ **Ecosystem thống nhất**: NuGet packages, tooling, deployment  

---

[⬅️ Quay lại README](../README.md)
