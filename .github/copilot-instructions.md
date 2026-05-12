# GitHub Copilot Instructions — WidgetData

## Tổng quan project
**WidgetData** là No-Code data pipeline platform. Admin tạo **Widget** để kéo dữ liệu từ nhiều nguồn (SQLite, CSV, JSON, Excel, REST API), xử lý qua multi-step pipeline, hiển thị real-time lên Blazor dashboard.

Stack: **.NET 10** · **Blazor Server** (MudBlazor) · **ASP.NET Core Web API** · **EF Core + SQLite** · **.NET Aspire** · **SignalR** · **Serilog** · **JWT Auth** · **Redis cache**

---

## Enums quan trọng

```csharp
enum DataSourceType { SqlServer, PostgreSql, MySql, SQLite, Csv, Excel, Json, RestApi }
enum WidgetType     { Chart, Table, Metric, Map, Form, EtlJob }
enum ExecutionStatus { Running, Success, Failed }
enum ExecutionTrigger { Manual, Scheduler }
```

---

## Kiến trúc — Clean Architecture

```
WidgetData.Domain          → Entities, Enums, Interfaces (không dependency ngoài)
WidgetData.Application     → DTOs, Interfaces, Helpers (dùng Domain)
WidgetData.Infrastructure  → EF Core, Repositories, Services (impl Application interfaces)
WidgetData.API             → ASP.NET Core Controllers, JWT, Rate Limiting (REST API)
WidgetData.Web             → Blazor Server admin dashboard (MudBlazor, SignalR)
WidgetData.Worker          → BackgroundService, SchedulerWorkerService (cron jobs)
WidgetData.Gateway         → API Gateway (YARP routing)
WidgetData.AppHost         → .NET Aspire orchestration (chạy tất cả service)
WidgetData.ServiceDefaults → Shared Aspire service configuration
```

**Dependency rule**: Domain ← Application ← Infrastructure ← API/Web/Worker

---

## Entities chính (Domain)

| Entity | Mô tả |
|--------|-------|
| `Widget` | Đơn vị trung tâm: có DataSourceId, Configuration (JSON), HtmlTemplate, TenantId, cache settings |
| `DataSource` | Nguồn dữ liệu: SQLite, CSV, JSON, Excel, REST API |
| `WidgetSchedule` | Cron schedule cho Widget (Cronos lib), RetryOnFailure, NextRunAt |
| `WidgetExecution` | Log mỗi lần chạy widget |
| `WidgetApiActivity` | Theo dõi mọi lần gọi API widget (endpoint, user, latency, status) |
| `Page` / `PageWidget` | Dashboard page ghép nhiều widget |
| `Tenant` | Multi-tenant: mỗi Widget/DataSource có TenantId |
| `WidgetGroup` / `WidgetGroupMember` | Nhóm widget, phân quyền |
| `DeliveryTarget` | Gửi kết quả qua email/webhook |
| `FormSubmission` | Submit từ Form Widget |

---

## Services đã đăng ký (Infrastructure DI)

- `IWidgetService` → `WidgetService` — execute widget, multi-step pipeline
- `IDataSourceService` → `DataSourceService` — đọc dữ liệu từ các nguồn
- `IScheduleService` → `ScheduleService` — quản lý cron
- `IDashboardService` → `DashboardService`
- `IPageService` → `PageService`
- `IFormService` → `FormService`
- `ITenantService` → `TenantService`
- `IWidgetActivityService` → `WidgetActivityService`
- `IExportService` → `ExportService` (QuestPDF)
- `IDeliveryService` → `DeliveryService`
- `IAuditService` → `AuditService`
- `IPermissionService` → `PermissionService`
- `InactivityMonitorService` → HostedService tự vô hiệu hoá widget không dùng

---

## Luồng chính

### Widget Execution (chi tiết thực tế trong `WidgetService.cs`)
```
API Request → WidgetsController → IWidgetService.ExecuteAsync(id, userId, scheduleId?)
  → GetByIdAsync → nếu scheduleId + ArchiveConfigOnRun=true → lưu WidgetConfigArchive
  → Tạo WidgetExecution { Status=Running }
  → GetDataAsync(id):
      switch ds.SourceType:
        SQLite  → validate query (chỉ SELECT, block INSERT/DROP/...) → SqliteConnection
        CSV     → CsvHelper đọc file từ ds.FileStoragePath
        JSON    → đọc file / gọi URL
        Excel   → ClosedXML đọc sheet đầu tiên
        RestApi → HttpClient GET ds.ApiEndpoint + Bearer ds.ApiKey
      → trả về { columns: string[], rows: object[][] }
  → Cập nhật WidgetExecution { Status=Success/Failed, RowCount, ExecutionTimeMs }
  → Cập nhật Widget.LastExecutedAt, LastRowCount
  → Return WidgetExecutionDto
```

### Scheduled Execution (Worker)
```
SchedulerWorkerService poll 30s → GetDueAsync(now)
  → RunScheduleAsync per schedule → IWidgetService.ExecuteAsync
  → On fail + RetryOnFailure=true → retry (MaxRetries, 5s delay)
  → Update LastRunAt, LastRunStatus, NextRunAt (Cronos)
  → SignalR broadcast
```

### Widget Update với Auto-Archive
```
UpdateAsync(id, dto):
  → So sánh Configuration / ChartConfig / HtmlTemplate cũ vs mới
  → Nếu có thay đổi → tự lưu WidgetConfigArchive (TriggerSource="OnSave")
  → Cập nhật widget fields → SaveChangesAsync
```

### Multi-Step Pipeline (Configuration JSON)
Widget.Configuration chứa JSON định nghĩa pipeline với các step types:
- `extract` — đọc dữ liệu (database/file/api/widget)
- `transform` — filter, add_column, rename, type conversion
- `join` — inner/left/right join, union
- `aggregate` — GROUP BY, SUM/AVG/COUNT/MIN/MAX
- `filter` — WHERE conditions, TOP N
- `branch_condition` — if/else dựa trên expression → true_branch / false_branch
- `branch_switch` — switch-case routing theo variable
- `merge` — gom kết quả nhiều nhánh (union_all)
- `output` — format, cache, webhook, save to DB

### Branching (Conditional Logic)
```json
{ "step_type": "branch_condition",
  "condition": { "expression": "total_spent > 10000",
                 "true_branch": 3, "false_branch": 5 } }
```
Các nhánh merge lại bằng step `merge` với `"mode": "union_all"`.

---

## Conventions

- **Repository pattern**: `IXxxRepository` → `XxxRepository` (scoped)
- **Service pattern**: `IXxxService` → `XxxService` (scoped)
- **Multi-tenancy**: Luôn filter theo `TenantId` (Shared DB). `ITenantContext` inject vào service để lấy `CurrentTenantId`. EF Global Query Filter tự lọc.
- **Kết quả data**: Tất cả nguồn trả về `{ columns: string[], rows: object[][] }`
- **Caching**: Check `Widget.CacheEnabled` + `CacheTtlMinutes` trước khi execute
- **SQL security**: Chỉ cho phép SELECT. Strip comment trước khi validate. Block: INSERT/UPDATE/DELETE/DROP/CREATE/ALTER/EXEC/TRUNCATE/MERGE/ATTACH
- **Config archive**: `WidgetService.UpdateAsync` tự archive config cũ khi Configuration/ChartConfig/HtmlTemplate thay đổi
- **Auth**: JWT Bearer, ASP.NET Identity, RBAC (`[Authorize(Roles = "...")]`). Password: 8+ ký tự, có digit, uppercase, special char. Lockout sau 5 lần sai.
- **Logging**: Serilog structured logging, ghi `WidgetExecution` và `AuditLog` vào DB
- **DB**: SQLite (dev), EF Core migrations trong `WidgetData.Infrastructure/Migrations/`
- **File storage**: DataSource file (CSV/Excel/JSON) lưu tại `ds.FileStoragePath`. Metadata: OriginalFileName, StoredFileName, FileSizeBytes, FileUploadedAt

---

## DataSource — cách đọc từng nguồn

| SourceType | Cách đọc | Config cần thiết |
|------------|----------|-----------------|
| `SQLite` | `Microsoft.Data.Sqlite`, chỉ SELECT | `ds.ConnectionString`, `widget.Configuration["query"]` |
| `Csv` | `CsvHelper` | `ds.FileStoragePath` |
| `Json` | Đọc file hoặc HTTP GET | `ds.FileStoragePath` hoặc `ds.ApiEndpoint` |
| `Excel` | `ClosedXML`, sheet đầu tiên | `ds.FileStoragePath` |
| `RestApi` | `HttpClient` GET + Bearer token | `ds.ApiEndpoint`, `ds.ApiKey` |

---

## Controllers (API)

`WidgetsController` · `DataSourcesController` · `SchedulesController` · `AuthController`
`PagesController` · `FormController` · `ReportsController` · `StoreController`
`TenantsController` · `WidgetGroupsController` · `PermissionsController`
`WidgetActivityController` · `AuditLogsController` · `DashboardController`
`DeliveryTargetsController` · `IdeaBoardController` · `WidgetConfigArchivesController`

---

## Libraries quan trọng

| Thư viện | Dùng cho |
|----------|----------|
| MudBlazor | Blazor UI components |
| Cronos | Parse cron expression, tính NextRunAt |
| CsvHelper | Đọc CSV |
| ClosedXML | Đọc/ghi Excel |
| QuestPDF | Export PDF |
| Serilog | Structured logging |
| SignalR | Real-time push |
| YARP | API Gateway routing |

---

## Tài liệu chi tiết

- `doc/architecture.md` — System design đầy đủ
- `doc/database-schema.md` — ERD và schema
- `doc/multi-step-processing.md` — Pipeline chi tiết
- `doc/branching-variables.md` — Branching if-else, switch-case, variables
- `doc/multi-tenancy.md` — ⚠️ Shared DB + TenantId, Global Query Filter
- `doc/security.md` — Auth, RBAC, encryption (8 lớp)
- `doc/api.md` — REST API reference
- `doc/deployment.md` — Cách chạy dev/Docker/Azure
