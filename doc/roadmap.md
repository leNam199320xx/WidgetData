# Roadmap & Tính năng tương lai

## Version 1.0 (MVP) - Q2 2026 ✅
- [x] Core widget engine (multi-source: SQLite, CSV, JSON, Excel, REST API)
- [x] **Standalone Cron Job Scheduler** — `WidgetData.Worker` (BackgroundService)
  - [x] Tách hoàn toàn khỏi API thành project riêng
  - [x] Cronos 0.8.4 — cron expression parser, timezone-aware `NextRunAt`
  - [x] `SchedulerWorkerService` — polling 30s, parallel execution, in-progress guard
  - [x] Retry support (`RetryOnFailure`, `MaxRetries`, 5s delay)
  - [x] `CronUtils` helper trong Infrastructure
  - [x] `IScheduleRepository.GetDueAsync` — query theo NextRunAt
  - [x] `ScheduleService` tự tính `NextRunAt` khi Create/Update/Trigger
- [x] In-memory caching
- [x] Blazor dashboard (MudBlazor)
- [x] .NET Aspire AppHost + YARP Gateway
- [x] JWT Authentication + ASP.NET Identity
- [x] REST API (Scalar docs)
- [x] HTML Designer + Dashboard Page Builder
- [x] Reports (doanh thu/bán hàng)
- [x] Store module (sản phẩm, đơn hàng)
- [x] Demo Shop (standalone HTML/JS + Blazor admin)
- [x] Form Widget (custom schema + submissions)
- [x] Activity Monitoring (auto-disable + InactivityAlert)

## Version 1.5 - Q3 2026
- [ ] Advanced charting (more chart types)
- [ ] Widget templates marketplace
- [ ] Excel formula support
- [ ] Advanced filtering & aggregations
- [ ] Mobile responsive improvements
- [ ] Dark mode theme
- [ ] Widget versioning & rollback

## Version 2.0 - Q4 2026
- [ ] **AI-Powered Features**:
  - Natural language query → Widget auto-generation
  - Anomaly detection in data
  - Predictive analytics
  - Smart recommendations
- [ ] **Collaboration**:
  - Team workspaces
  - Widget sharing & permissions
  - Comments & annotations
  - Activity feed
- [ ] **Advanced Integrations**:
  - Power BI connector
  - Tableau integration
  - Slack/Teams bots
  - Zapier integration

## Version 2.5 - Q1 2027
- [ ] **Data Transformation**:
  - Visual ETL builder
  - Data pipeline orchestration
  - Data quality checks
  - Master data management
- [ ] **Enterprise Features**:
  - Multi-tenancy
  - SSO (SAML, OIDC)
  - Audit trail & compliance
  - Custom branding
  - SLA monitoring

## Version 3.0 - Q2 2027
- [ ] **Embedded Analytics**:
  - Embeddable widgets (iframe)
  - Public dashboards
  - White-label solution
  - Widget marketplace
- [ ] **Machine Learning**:
  - ML model integration
  - Auto-scaling predictions
  - Data clustering
  - Trend forecasting

## Future Considerations
- [ ] Mobile apps (iOS/Android)
- [ ] Real-time collaboration (Google Docs-style)
- [ ] Blockchain data sources
- [ ] GraphQL API
- [ ] Kubernetes deployment
- [ ] Multi-language support (i18n)

## Community Requests
📋 [Vote for features](https://github.com/your-org/widget-data/discussions)
- Widget scheduling calendar view
- Drag-drop dashboard builder
- Custom widget themes
- Data export to Google Sheets
- Conditional formatting

---

[⬅️ Quay lại README](../README.md)
