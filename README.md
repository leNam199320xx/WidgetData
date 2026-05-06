# Widget Data

> 🚀 Data Pipeline & Widget Platform - Read, Transform, Visualize data từ nhiều nguồn với No-Code approach

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![Aspire](https://img.shields.io/badge/.NET_Aspire-AppHost-512BD4?logo=dotnet)](https://learn.microsoft.com/en-us/dotnet/aspire/)

## 📋 Tổng quan

Widget Data là platform cho phép bạn:
- 📊 **Đọc dữ liệu** từ database, files (CSV/JSON/Excel), REST APIs — tất cả trả về cùng định dạng
- 🔄 **Xử lý dữ liệu** qua multi-step pipeline (Extract → Transform → Aggregate → Load)
- 🔀 **Conditional logic** với branching (if-else, switch-case, parallel execution)
- 📅 **Tự động hóa** với scheduling (cron, interval, event-driven)
- ⚡ **Cache & optimize** với multi-level caching
- 📈 **Visualize real-time** trên Blazor dashboard với SignalR
- 🎨 **HTML Designer** tạo giao diện tùy chỉnh với template engine
- 📑 **Dashboard Pages** ghép nhiều widget thành trang báo cáo
- 📝 **Form Widget** — thu thập dữ liệu với schema tùy chỉnh, lưu submission vào DB
- 📡 **Activity Monitoring** — theo dõi mọi lần gọi API widget, tự vô hiệu hoá khi không hoạt động
- 🌐 **Standalone Frontend** — public-facing pages từ WidgetEngine, tách hoàn toàn khỏi Blazor
- 🔐 **Bảo mật** với authentication, authorization, encryption

**Không cần code!** Business users có thể tạo data pipelines qua visual builder.

## ✨ Tính năng chính

- **Multi-Step Processing**: Pipeline với nhiều bước xử lý tuần tự → [📖 Chi tiết](doc/multi-step-processing.md)
- **Branching & Variables**: If-else, switch-case, parallel branches + biến → [📖 Chi tiết](doc/branching-variables.md)
- **Multi-Source Data**: Đọc dữ liệu từ **SQLite/SQL**, **CSV**, **JSON**, **Excel** (ClosedXML), **REST API** — tất cả trả về cùng định dạng `{ columns, rows }`
- **Form Widget**: Admin định nghĩa schema field (text, email, textarea, select…); `/api/form/{id}/schema` trả schema public; submission lưu DB qua `POST /api/form/{id}`; admin xem qua `GET /api/form/{id}/submissions`
- **HTML Widget Designer**: Thiết kế template HTML tùy chỉnh với biến `{{column}}` và vòng lặp `{{#each rows}}`
- **Dashboard Page Builder**: Kéo-thả widget thành trang dashboard, xem trước trực tiếp
- **Reports & Preview**: Trang báo cáo doanh thu/bán hàng từ dữ liệu thực tế
- **Widget Activity Monitoring**: Tự động ghi lại mọi lần gọi API widget (endpoint, user, thời gian phản hồi, status code); background service phát hiện widget không hoạt động → tự vô hiệu hoá + ghi alert log
- **Scheduling**: Cron job tự động qua **WidgetData.Worker** (BackgroundService + Cronos), hỗ trợ retry, NextRunAt, timezone
- **Caching**: In-memory, Redis, file-based cache với TTL và invalidation
- **Live Data**: Real-time dashboard qua SignalR, auto-refresh
- **Security**: ASP.NET Identity, JWT, MFA, RBAC, encryption → [📖 Chi tiết](doc/security.md)
- **Blazor UI**: Modern dashboard với MudBlazor, charts, code editor
- **Store Module**: Quản lý sản phẩm, đơn hàng, thanh toán — giao diện tách biệt với `StoreLayout`
- **Demo Storefront** (`demo/shop-front/`): trang bán hàng public HTML/CSS/JS thuần, render từ config xuất bởi **WidgetData.Web** (Blazor admin shop)

## 🎯 Use Cases

### 1. Sales Report Pipeline
```
Step 1: Đọc orders từ SQLite
Step 2: Đọc products từ CSV  
Step 3: Join orders + products
Step 4: Aggregate revenue by category
Step 5: Filter top 10
Step 6: Cache & display
```

### 2. Customer Segmentation với Branching
```
IF customer.total_spent > 10000:
    → VIP processing (15% discount)
ELSE:
    → Standard processing (5% discount)
→ Merge results
```

### 3. ETL Pipeline
```
Extract từ legacy DB → Clean data → Transform format → Load to warehouse
```

### 4. HTML Widget Report
```
Tạo HTML template: <table>{{#each rows}}<tr><td>{{product}}</td><td>{{revenue}}</td></tr>{{/each}}</table>
Widget tự điền dữ liệu thực tế → Dashboard Page → Xuất PDF/HTML
```

### 5. Form Widget — Thu thập dữ liệu
```
Admin định nghĩa schema: email, textarea, select…
/form.html tự render form → POST /api/form/{id} → lưu FormSubmission
Admin xem submissions qua GET /api/form/{id}/submissions
```

### 6. Widget Activity Monitoring
```
Mọi lần gọi /api/widgets/{id}/execute|data|export đều được ghi WidgetApiActivity
Background service chạy mỗi 60 phút → phát hiện widget không hoạt động > N ngày
→ Tự vô hiệu hoá widget (nếu bật) + ghi InactivityAlert vào AuditLog
```

## 🚀 Quick Start

### Prerequisites
- .NET 10.0 SDK
- Docker Desktop (cho .NET Aspire AppHost)
- Visual Studio 2022 / VS Code / Rider

### Installation

```bash
# Clone repository
git clone https://github.com/your-org/widget-data.git
cd widget-data

# Restore packages
dotnet restore

# Chạy toàn bộ hệ thống qua .NET Aspire (API + Worker + Web + Gateway + Demos)
dotnet run --project src/WidgetData.AppHost

# Hoặc chạy riêng từng service
dotnet run --project src/WidgetData.Web
dotnet run --project src/WidgetData.API
dotnet run --project src/WidgetData.Worker
```

Truy cập:
- **Admin Dashboard** (Blazor): `https://localhost:5001`
- **Demo Storefront** (trang bán hàng public): `npx serve demo/storefront` hoặc mở `demo/shop-front/index.html`
- **API Gateway** (YARP): `https://localhost:7000`
- **API Swagger**: `https://localhost:7001/swagger`
- **.NET Aspire Dashboard**: `https://localhost:15888`

👉 [Hướng dẫn cài đặt chi tiết](doc/deployment.md)

## 📐 Architecture

```
┌─────────────────────────────────────────────────────────┐
│  TẦNG 1 — PLATFORM  (src/)                              │
│                                                         │
│  .NET Aspire AppHost + Gateway (YARP)                   │
│                                                         │
│  WidgetData.Web (Blazor)                                │
│    Admin platform: widget builder, HTML designer        │
│    Dashboard page builder, reports, data pipeline       │
│                                                         │
│  WidgetData.API (ASP.NET Core)                          │
│    Execute widget | schedule | cache | auth | reports   │
│                         ↓                               │
│  WidgetData.Worker (BackgroundService)                  │
│    Cron job executor | NextRunAt | retry | timezone     │
│                         ↓                               │
│  EF Core + SQLite | Cronos | Redis | SignalR             │
└─────────────────────────┬───────────────────────────────┘
                          │ Platform deploy cho đơn vị nghiệp vụ
┌─────────────────────────▼───────────────────────────────┐
│  TẦNG 2 — BUSINESS APP  (demo/)                         │
│                                                         │
│  shop-admin/    ← Backend quản lý shop                  │
│    WidgetData.Web cấu hình cho nghiệp vụ bán hàng       │
│    Quản lý sản phẩm, đơn hàng, KH, báo cáo              │
│    Xuất page config JSON → storefront                   │
│                                                         │
│  shop-front/    ← Trang bán hàng public                 │
│    HTML/CSS/JS thuần (zero .NET dependency)             │
│    WidgetEngine đọc JSON config → render UI             │
│    Deploy độc lập: CDN / nginx / GitHub Pages           │
└─────────────────────────────────────────────────────────┘
```

👉 [Chi tiết Architecture](doc/architecture.md)

## 💻 Technology Stack

**Backend**: ASP.NET Core 10.0, EF Core, Cronos, SignalR, QuestPDF, ClosedXML  
**Worker**: .NET Worker Service, Cronos 0.8.4, BackgroundService (SchedulerWorkerService)  
**Frontend (Blazor)**: Blazor Server, MudBlazor, ChartJs, BlazorMonaco  
**Frontend (Standalone)**: Vanilla HTML/CSS/JS, WidgetEngine library (zero-dep)  
**Infrastructure**: .NET Aspire, YARP Gateway, SQLite, Docker, Serilog  

👉 [Chi tiết Technology](doc/architecture.md#technology-stack)

## 🔌 API Endpoints (tóm tắt)

| Nhóm | Endpoint | Quyền | Mô tả |
|---|---|---|---|
| **Widget** | `POST /api/widgets/{id}/execute` | Auth | Thực thi widget, ghi `WidgetExecution` |
| **Widget** | `GET /api/widgets/{id}/data` | Auth | Lấy dữ liệu theo nguồn (DB/CSV/JSON/Excel/API) |
| **Form** | `GET /api/form/{id}/schema` | Public | Lấy schema field của Form widget |
| **Form** | `POST /api/form/{id}` | Public | Gửi submission; validate required fields |
| **Form** | `GET /api/form/{id}/submissions` | Admin/Manager | Xem danh sách submission |
| **Form** | `DELETE /api/form/submissions/{id}` | Admin | Xoá submission |
| **Activity** | `GET /api/widget-activity/{widgetId}` | Admin/Manager | Log hoạt động API theo widget |
| **Activity** | `GET /api/widget-activity/{widgetId}/summary` | Admin/Manager | Tổng hợp: tổng lời gọi, user, top endpoints |
| **Activity** | `GET /api/widget-activity/inactive` | Admin | Danh sách widget không hoạt động |
| **Activity** | `GET /api/widget-activity/alerts` | Admin | Tất cả `InactivityAlert` trong AuditLog |

## ⚙️ Cấu hình

### SchedulerWorker (`appsettings.json`)

```json
"SchedulerWorker": {
  "PollingIntervalSeconds": 30
}
```

`WidgetData.Worker` polling mỗi `PollingIntervalSeconds` giây. Schedule nào có `IsEnabled = true` và `NextRunAt <= now` sẽ được tự động thực thi. Sau mỗi lần chạy, `NextRunAt` được tính lại từ cron expression + timezone. Nếu `RetryOnFailure = true`, service sẽ retry tối đa `MaxRetries` lần (5 giây giữa mỗi lần).

### Inactivity Monitor (`appsettings.json`)

```json
"InactivityMonitor": {
  "CheckIntervalMinutes": 60,
  "DefaultThresholdDays": 30
}
```

Background service chạy theo `CheckIntervalMinutes`. Widget nào có `InactivityAutoDisableEnabled = true` và không được gọi trong `InactivityThresholdDays` ngày sẽ bị đặt `IsActive = false` tự động.

### Data Source — Multi-Source Config

| Loại nguồn | Config cần thiết |
|---|---|
| `Database` | `ConnectionString` (SQLite/SQL), `Query` |
| `CSV` | `ConnectionString` = đường dẫn file; `delimiter`, `hasHeader` |
| `JSON` | `ConnectionString` = đường dẫn file; `jsonPath` (nested array) |
| `Excel` | `ConnectionString` = đường dẫn file; `sheet`, `hasHeader` |
| `RestApi` | `ApiEndpoint`, `ApiKey`; `jsonPath` |

## 📚 Documentation

### 📖 Core Features
- [Multi-Step Data Processing](doc/multi-step-processing.md) - Pipeline ETL
- [Branching & Variables](doc/branching-variables.md) - Conditional logic
- [Kiến trúc & Thiết kế](doc/architecture.md) - System architecture
- [Module & Thư viện](doc/modules.md) - 70% có sẵn!

### 🔌 API & Database
- [API Reference](doc/api.md) - REST API endpoints, request/response, error codes
- [Database Schema](doc/database-schema.md) - ERD, bảng, indexes, migrations

### 🔐 Security & Deployment
- [Bảo mật](doc/security.md) - Auth, Authorization, Encryption
- [Cài đặt & Triển khai](doc/deployment.md) - Dev/Docker/Azure
- [Performance](doc/performance.md) - Caching, Optimization
- [Monitoring](doc/monitoring.md) - Logging, Metrics

### 🧪 Testing & Support
- [Testing](doc/testing.md) - Unit, Integration tests
- [Backup & DR](doc/backup.md) - High Availability
- [Troubleshooting](doc/troubleshooting.md) - FAQ

### 🔌 Integration
- [Tích hợp & Mở rộng](doc/integration.md) - Webhooks, Plugins
- [Mobile & Responsive](doc/mobile.md) - PWA, Offline

### 🗺️ Planning
- [Roadmap](doc/roadmap.md) - v1.0 → v3.0
- [Screens & UI](doc/screens.md) - Tất cả màn hình
- [📑 Tài liệu đầy đủ](doc/INDEX.md) - All docs

## 📊 Example: Multi-Step Widget

```json
{
  "widget_name": "MonthlyRevenue",
  "steps": [
    {
      "step_id": 1,
      "step_type": "extract",
      "source": { "type": "database", "query": "SELECT * FROM orders" },
      "output": "orders"
    },
    {
      "step_id": 2,
      "step_type": "join",
      "join_config": { "left": "orders", "right": "products", "on": "product_id" },
      "output": "orders_with_products"
    },
    {
      "step_id": 3,
      "step_type": "aggregate",
      "aggregate_config": {
        "group_by": ["category"],
        "aggregations": [{ "column": "total", "function": "sum", "alias": "revenue" }]
      },
      "output": "revenue_by_category"
    }
  ]
}
```

👉 [Xem thêm examples](doc/multi-step-processing.md#ví-dụ-cấu-hình)

## 🖌️ Example: HTML Widget Template

```html
<!-- Widget HTML Template – biến {{column}} tự động thay thế bằng dữ liệu -->
<table class="report-table">
  <thead><tr><th>Sản phẩm</th><th>Doanh thu</th></tr></thead>
  <tbody>
    {{#each rows}}
    <tr>
      <td>{{product_name}}</td>
      <td>{{revenue}}</td>
    </tr>
    {{/each}}
  </tbody>
</table>
```

## 📝 Example: Form Widget Schema

```json
{
  "fields": [
    { "name": "email",   "label": "Email",    "type": "email",    "required": true  },
    { "name": "message", "label": "Nội dung", "type": "textarea", "required": false }
  ],
  "submitLabel": "Gửi",
  "successMessage": "Cảm ơn bạn đã gửi thông tin!"
}
```

Lưu config này vào `Widget.Configuration`. Trang `/form.html` (wwwroot) tự render form từ `GET /api/form/{id}/schema` và submit qua `POST /api/form/{id}`. Có thể nhúng vào bất kỳ trang HTML nào qua `<iframe>`.

## 🚦 Roadmap

- ✅ **v1.0** (Q2 2026) - MVP: Core widgets, scheduling với **WidgetData.Worker** (cron job project riêng, NextRunAt, retry, timezone), Blazor UI, .NET Aspire, HTML Designer, Dashboard Pages, Reports, Store module, Demo Shop (standalone + blazor-web), **Multi-source data** (CSV/JSON/Excel/API), **Form widget** (custom schema + submissions), **Activity Monitoring** (auto-disable + alert log)
- 🔄 **v1.5** (Q3 2026) - Advanced charts, templates
- 📅 **v2.0** (Q4 2026) - AI features, Power BI integration
- 📅 **v2.5** (Q1 2027) - Visual ETL, multi-tenancy
- 📅 **v3.0** (Q2 2027) - Embedded analytics, ML

👉 [Chi tiết Roadmap](doc/roadmap.md)

## 🤝 Contributing

Contributions are welcome! 

```bash
git checkout -b feature/amazing-feature
git commit -m 'Add amazing feature'
git push origin feature/amazing-feature
```

Open Pull Request on GitHub.

## 📝 License

MIT License - see [LICENSE](LICENSE)

## 📧 Contact

- **Issues**: [GitHub Issues](https://github.com/your-org/widget-data/issues)
- **Discussions**: [GitHub Discussions](https://github.com/your-org/widget-data/discussions)

---

**Made with ❤️ using .NET, Blazor & Vanilla JS**

