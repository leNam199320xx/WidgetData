# Widget Data

> 🚀 Data Pipeline & Widget Platform - Read, Transform, Visualize data từ nhiều nguồn với No-Code approach

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)

## 📋 Tổng quan

Widget Data là platform cho phép bạn:
- 📊 **Đọc dữ liệu** từ database, files (CSV/JSON/Excel), APIs
- 🔄 **Xử lý dữ liệu** qua multi-step pipeline (Extract → Transform → Aggregate → Load)
- 🔀 **Conditional logic** với branching (if-else, switch-case, parallel execution)
- 📅 **Tự động hóa** với scheduling (cron, interval, event-driven)
- ⚡ **Cache & optimize** với multi-level caching
- 📈 **Visualize real-time** trên Blazor dashboard với SignalR
- 🔐 **Bảo mật** với authentication, authorization, encryption

**Không cần code!** Business users có thể tạo data pipelines qua visual builder.

## ✨ Tính năng chính

- **Multi-Step Processing**: Pipeline với nhiều bước xử lý tuần tự → [📖 Chi tiết](doc/multi-step-processing.md)
- **Branching & Variables**: If-else, switch-case, parallel branches + biến → [📖 Chi tiết](doc/branching-variables.md)
- **Scheduling**: Hangfire scheduler với cron expressions, interval, on-demand
- **Caching**: In-memory, Redis, file-based cache với TTL và invalidation
- **Live Data**: Real-time dashboard qua SignalR, auto-refresh
- **Security**: ASP.NET Identity, JWT, MFA, RBAC, encryption → [📖 Chi tiết](doc/security.md)
- **Blazor UI**: Modern dashboard với MudBlazor, charts, code editor

## 🎯 Use Cases

### 1. Sales Report Pipeline
```
Step 1: Đọc orders từ SQL Server
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

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- SQL Server 2019+ (hoặc PostgreSQL/MySQL)  
- Redis (optional)
- Visual Studio 2022 / VS Code

### Installation

```bash
# Clone repository
git clone https://github.com/your-org/widget-data.git
cd widget-data

# Restore packages
dotnet restore

# Update database
dotnet ef database update --project src/WidgetData.Infrastructure

# Run application
dotnet run --project src/WidgetData.Web
```

Truy cập:
- Frontend: `https://localhost:5001`
- Hangfire: `https://localhost:5001/hangfire`
- API Swagger: `https://localhost:7001/swagger`

👉 [Hướng dẫn cài đặt chi tiết](doc/deployment.md)

## 📐 Architecture

```
┌─────────────────────────────────────┐
│     BLAZOR FRONTEND                 │
│  Dashboard | Widget Builder         │
└──────────────┬──────────────────────┘
               │ SignalR + REST API
┌──────────────┴──────────────────────┐
│     ASP.NET CORE WEB API            │
│  Widget API | Auth | Source API     │
└──────────────┬──────────────────────┘
               │
┌──────────────┴──────────────────────┐
│  Services: Widget | Schedule | Cache│
└──────────────┬──────────────────────┘
               │
┌──────────────┴──────────────────────┐
│  Infra: EF Core | Hangfire | Redis  │
└──────────────┬──────────────────────┘
               │
┌──────────────┴──────────────────────┐
│  Data: SQL | Files | APIs           │
└─────────────────────────────────────┘
```

👉 [Chi tiết Architecture](doc/architecture.md)

## 💻 Technology Stack

**Backend**: ASP.NET Core 8.0, EF Core, Hangfire, SignalR, Redis  
**Frontend**: Blazor Server/WASM, MudBlazor, ChartJs, BlazorMonaco  
**Infrastructure**: Docker, Azure App Service, SQL Server, Serilog  

👉 [Chi tiết Technology](doc/architecture.md#technology-stack)

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

## 🚦 Roadmap

- ✅ **v1.0** (Q2 2026) - MVP: Core widgets, scheduling, Blazor UI
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

**Made with ❤️ using .NET & Blazor**
