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
                          │ Shared Infrastructure (EF Core, Repos)
┌─────────────────────────┴────────────────────────────────────────┐
│                  WIDGETDATA.WORKER (BackgroundService)            │
│  SchedulerWorkerService — polling 30s                            │
│  GetDueAsync → ExecuteAsync → update NextRunAt / LastRunStatus   │
│  Retry support (MaxRetries, 5s delay) | In-progress guard        │
└─────────────────────────┬────────────────────────────────────────┘
                          │
┌─────────────────────────┴────────────────────────────────────────┐
│                   INFRASTRUCTURE LAYER                           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │ EF Core      │  │   Cronos     │  │    Redis     │          │
│  │ Repository   │  │  (NextRunAt) │  │    Cache     │          │
│  └──────┬───────┘  └──────┬───────┘  └──────────────┘          │
└─────────┼──────────────────┼──────────────────────────────────────┘
          │                  │
┌─────────┴──────────────────┴──────────────────────────────────────┐
│                      DATA SOURCES                                 │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐           │
│  │  SQLite  │ │   Files  │ │  Excel   │ │   APIs   │           │
│  │          │ │ CSV/JSON │ │(ClosedXML│ │ REST/etc │           │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘           │
└───────────────────────────────────────────────────────────────────┘
```

## Luồng dữ liệu

### 1. Luồng thực thi Widget
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

### 2. Luồng Widget theo lịch
```
WidgetData.Worker — SchedulerWorkerService (BackgroundService)
         ↓ poll every 30s
  GetDueAsync(now): IsEnabled=true AND NextRunAt <= now
         ↓
  RunScheduleAsync (per-schedule, parallel, in-progress guard)
         ↓
  WidgetService.ExecuteAsync(widgetId, "scheduler", scheduleId)
         ↓
  Execute Widget → Log WidgetExecution
         ↓
  On failure + RetryOnFailure=true → retry up to MaxRetries (5s delay)
         ↓
  Update LastRunAt / LastRunStatus
  Recalculate NextRunAt = CronUtils.GetNextOccurrence(cron, timezone, now)
         ↓
  SignalR Broadcast (to live subscribers)
```

## Kiến trúc phân lớp

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
│ Application Layer (Use-case Interfaces)                   │
│ - IWidgetService, IPageService, IDeliveryService, etc.    │
└───────────────────────────┬───────────────────────────────┘
                             │
┌───────────────────────────┴───────────────────────────────┐
│ Modular Monolith — Infrastructure Layer                   │
│                                                           │
│  Widgets Module                                           │
│    WidgetCrudService      → CRUD widget                   │
│    WidgetExecutionService → execute, history, data        │
│    Strategies: Csv / Json / Excel / RestApi               │
│                                                           │
│  DataSources Module                                       │
│    DataSourceCrudService           → CRUD data source     │
│    DataSourceUploadService         → file upload          │
│    DataSourceConnectivityTestService → test connection    │
│    Validators: Csv / Json / Excel / RestApi               │
│                                                           │
│  Pages Module                                             │
│    PageCrudService          → CRUD page                   │
│    PageVersioningService    → publish/rollback/snapshot   │
│    PageLayoutService        → widget layout               │
│                                                           │
│  Delivery Module                                          │
│    DeliveryTargetService    → target CRUD                 │
│    DeliveryExecutionService → execution history           │
│    DeliveryDispatcher       → orchestrates delivery       │
│    Channels: Email / SFTP / SSH / HTTP / Telegram / Zalo / File │
│                                                           │
│  Identity Module                                          │
│    User, Role, Tenant, Permission management              │
│                                                           │
│  Cross-cutting Module                                     │
│    Audit, Logging, Seeding, Migration, Shared Abstractions│
└───────────────────────────┬───────────────────────────────┘
                             │
┌───────────────────────────┴───────────────────────────────┐
│ Data Access Layer (Repositories)                          │
│ - EF Core (SQLite/PostgreSQL)                             │
│ - JSON File-backed Repositories                           │
│ - Repository interfaces per aggregate root                │
└───────────────────────────┬───────────────────────────────┘
                             │
┌───────────────────────────┴───────────────────────────────┐
│ Database & External Sources                               │
│ - SQLite / PostgreSQL                                     │
│ - CSV / JSON / Excel files                                │
│ - REST APIs                                               │
└───────────────────────────────────────────────────────────┘
```

## Mẫu Thiết kế Component

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
public interface IDataSourceStrategy {
    DataSourceType SourceType { get; }
    Task LoadDataAsync(int widgetId, DataSource dataSource);
}

// Implementations:
- CsvDataSourceStrategy
- JsonDataSourceStrategy
- ExcelDataSourceStrategy
- RestApiDataSourceStrategy
```

### 3. Strategy Pattern (Delivery Channels)
```csharp
public interface IDeliveryChannelStrategy {
    DeliveryType SupportedType { get; }
    Task DeliverAsync(int widgetId, DeliveryTarget target, IExportService exportService, IHttpClientFactory httpClientFactory);
}

// Implementations:
- EmailDeliveryChannelStrategy
- SftpDeliveryChannelStrategy
- SshDeliveryChannelStrategy
- HttpApiDeliveryChannelStrategy
- TelegramDeliveryChannelStrategy
- ZaloDeliveryChannelStrategy
- FileDeliveryChannelStrategy
```

### 4. Facade Pattern (Backward Compatibility)
```csharp
// Old interface preserved
public interface IWidgetService {
    Task<IEnumerable<WidgetDto>> GetAllAsync();
    Task<WidgetDto?> GetByIdAsync(int id);
    Task<WidgetDto> CreateAsync(CreateWidgetDto dto);
    Task<WidgetDto?> UpdateAsync(int id, UpdateWidgetDto dto);
    Task<bool> DeleteAsync(int id);
    Task<WidgetExecutionResult> ExecuteAsync(int id, string trigger);
    Task<IEnumerable<WidgetExecutionDto>> GetHistoryAsync(int id);
}

// Facade delegates to focused services
public class WidgetService : IWidgetService {
    private readonly IWidgetCrudService _crud;
    private readonly IWidgetExecutionService _execution;
    
    public Task<WidgetDto> CreateAsync(CreateWidgetDto dto) 
        => _crud.CreateAsync(dto);
    public Task<WidgetExecutionResult> ExecuteAsync(int id, string trigger) 
        => _execution.ExecuteAsync(id, trigger);
}
```

### 5. Startup Initializer Pipeline
```csharp
public interface IStartupInitializer {
    string Name { get; }
    int Order { get; }
    Task InitializeAsync(IServiceProvider serviceProvider);
}

// Implementations run in order:
- DatabaseSchemaInitializer (Order=1) — ensures DB schema is ready
- DataSeedInitializer (Order=2) — seeds roles, tenants, users, demo data
```

## Công nghệ sử dụng

### Backend (.NET Core)
- **Framework**: ASP.NET Core 10.0
- **API**: ASP.NET Core Web API (REST)
- **ORM**: Entity Framework Core 10.0
- **Database**: SQLite (primary)
- **Cron Scheduler**: **WidgetData.Worker** — .NET Worker Service + **Cronos 0.8.4** (cron expression parser, NextRunAt calculation)
- **Cache**: In-Memory Cache (IMemoryCache)
- **File Processing**: ClosedXML (Excel), System.Text.Json, SSH.NET
- **PDF**: QuestPDF
- **Email**: MailKit/MimeKit
- **Telegram**: Telegram.Bot
- **Real-time**: SignalR
- **Authentication**: ASP.NET Core Identity + JWT
- **Logging**: Serilog

### Frontend (Blazor)
- **Framework**: Blazor Web App (.NET 10)
- **UI Components**: MudBlazor
- **Charts**: ChartJs.Blazor
- **Real-time**: SignalR (native integration)
- **Code Editor**: BlazorMonaco (SQL/JSON editing)

### Lưu trữ & Hạ tầng
- **Database**: SQLite (EF Core)
- **Logging**: Serilog → Console / File
- **Orchestration**: .NET Aspire AppHost
- **Gateway**: YARP (WidgetData.Gateway)
- **Deployment**: .NET Aspire / Docker

## Packages chính (.NET)

```
# WidgetData.Infrastructure
Microsoft.AspNetCore.Identity.EntityFrameworkCore
Microsoft.EntityFrameworkCore.Sqlite
ClosedXML
Cronos (0.8.4) — cron expression parser, NextRunAt calculation
Hangfire.Core / Hangfire.InMemory — retained for background job utilities
MailKit / MimeKit
QuestPDF
Serilog.AspNetCore
SSH.NET
Telegram.Bot

# WidgetData.API
Microsoft.AspNetCore.Authentication.JwtBearer
Microsoft.AspNetCore.OpenApi
Scalar.AspNetCore (API docs)
Serilog.AspNetCore

# WidgetData.Worker (BackgroundService — cron job executor)
Microsoft.NET.Sdk.Worker
Serilog.AspNetCore
(references Infrastructure + ServiceDefaults)

# WidgetData.Web (Blazor)
MudBlazor
ChartJs.Blazor / ApexCharts
BlazorMonaco

# WidgetData.Gateway
Microsoft.ReverseProxy (YARP)

# WidgetData.ServiceDefaults (.NET Aspire shared)
OpenTelemetry.Extensions.Hosting
Microsoft.Extensions.Http.Resilience
Microsoft.Extensions.ServiceDiscovery
```

## Lợi thế của stack Full .NET (Backend + Blazor)

✅ **Cùng ngôn ngữ C#**: Share code giữa backend/frontend (models, validation, logic)  
✅ **Type safety**: Strongly typed từ đầu đến cuối  
✅ **SignalR native**: Real-time integration hoàn hảo cho live data  
✅ **Performance**: Blazor Server rất nhanh, Blazor WASM chạy gần native speed  
✅ **Productivity cao**: Không cần context switching giữa C# và JavaScript  
✅ **Debugging tốt**: F5 debug cả backend lẫn frontend trong Visual Studio  
✅ **Ecosystem thống nhất**: NuGet packages, tooling, deployment  

## Các API Endpoint

### Widget API

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| `GET` | `/api/widgets` | Lấy danh sách widgets | ✅ |
| `GET` | `/api/widgets/{id}` | Lấy chi tiết widget | ✅ |
| `POST` | `/api/widgets` | Tạo widget mới | Admin/Manager |
| `PUT` | `/api/widgets/{id}` | Cập nhật widget | Owner/Admin |
| `DELETE` | `/api/widgets/{id}` | Xóa widget | Admin |
| `POST` | `/api/widgets/{id}/execute` | Thực thi widget | ✅ |
| `GET` | `/api/widgets/{id}/data` | Lấy dữ liệu widget | ✅ |
| `GET` | `/api/widgets/{id}/history` | Lịch sử thực thi | ✅ |

### Data Source API

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| `GET` | `/api/datasources` | Danh sách data sources | ✅ |
| `GET` | `/api/datasources/{id}` | Chi tiết data source | ✅ |
| `POST` | `/api/datasources` | Tạo data source | Admin/Manager |
| `PUT` | `/api/datasources/{id}` | Cập nhật data source | Owner/Admin |
| `DELETE` | `/api/datasources/{id}` | Xóa data source | Admin |
| `POST` | `/api/datasources/{id}/test` | Test kết nối | ✅ |

### Schedule API

| Method | Endpoint | Mô tả | Auth |
|--------|----------|-------|------|
| `GET` | `/api/schedules` | Danh sách schedules | ✅ |
| `POST` | `/api/schedules` | Tạo schedule (NextRunAt tự tính) | Manager/Admin |
| `PUT` | `/api/schedules/{id}` | Cập nhật schedule (NextRunAt tự tính lại) | Owner/Admin |
| `DELETE` | `/api/schedules/{id}` | Xóa schedule | Admin |
| `POST` | `/api/schedules/{id}/enable` | Kích hoạt (NextRunAt tính lại) | Manager/Admin |
| `POST` | `/api/schedules/{id}/disable` | Tắt | Manager/Admin |
| `POST` | `/api/schedules/{id}/trigger` | Chạy thủ công ngay lập tức | Manager/Admin |

### Auth API

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| `POST` | `/api/auth/login` | Đăng nhập → JWT token |
| `POST` | `/api/auth/refresh` | Làm mới JWT token |
| `POST` | `/api/auth/logout` | Đăng xuất |
| `POST` | `/api/auth/register` | Đăng ký tài khoản |
| `POST` | `/api/auth/forgot-password` | Quên mật khẩu |

```csharp
// Ví dụ request/response
// POST /api/widgets/{id}/execute
// Request:
{
  "parameters": {
    "start_date": "2026-01-01",
    "end_date": "2026-12-31"
  }
}
// Response:
{
  "widget_id": 123,
  "execution_id": "exec-456",
  "status": "success",
  "data": [...],
  "row_count": 150,
  "execution_time_ms": 342,
  "cached": false
}
```

👉 [Chi tiết API Reference](api.md)

---

[⬅️ Quay lại README](../README.md)
