# GitHub Copilot Instructions — WidgetData

## Build, test, and hygiene commands

```powershell
dotnet restore WidgetData.sln
dotnet build WidgetData.sln --configuration Release --no-restore

# Run the full stack through .NET Aspire
dotnet run --project src\WidgetData.AppHost

# Run individual services
dotnet run --project src\WidgetData.API
dotnet run --project src\WidgetData.Web
dotnet run --project src\WidgetData.Worker
dotnet run --project src\WidgetData.Gateway

# Unit tests
dotnet test tests\WidgetData.Tests\WidgetData.Tests.csproj --configuration Release --no-build

# Integration tests
dotnet test tests\WidgetData.IntegrationTests\WidgetData.IntegrationTests.csproj --configuration Release --no-build

# Run a single xUnit test
dotnet test tests\WidgetData.Tests\WidgetData.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~WidgetData.Tests.Services.WidgetServiceTests.GetAllAsync_ReturnsAllWidgets"

# Run a single integration test
dotnet test tests\WidgetData.IntegrationTests\WidgetData.IntegrationTests.csproj --configuration Release --no-build --filter "FullyQualifiedName~WidgetData.IntegrationTests.ApiSmokeTests.Health_Endpoint_Should_ReturnSuccess"

# CI coverage run
dotnet test WidgetData.sln --configuration Release --no-build --collect:"XPlat Code Coverage" --results-directory TestResults

# No dedicated linter is configured; the extra CI hygiene check is package vulnerability scanning
dotnet list WidgetData.sln package --vulnerable --include-transitive
```

CI currently enforces a combined coverage floor of **20%** from the Cobertura files produced by the coverage run above.

## High-level architecture

- **Clean architecture with explicit project references.** `WidgetData.Domain` is the base layer, `WidgetData.Application` holds DTOs/interfaces, `WidgetData.Infrastructure` implements repositories and services, and the executable apps (`API`, `Web`, `Worker`, `Gateway`, `AppHost`) sit on top.
- **AppHost is the runtime entry point for local full-stack work.** `src\WidgetData.AppHost\Program.cs` wires up `widgetdata-api`, `widgetdata-worker`, `widgetdata-gateway`, and `widgetdata-web`; `WidgetData.ServiceDefaults` adds service discovery, resilience handlers, OpenTelemetry, and `/health` + `/alive`.
- **The Blazor app is an admin client, not the business-logic host.** `WidgetData.Web` uses `ApiService` with the service-discovery base address `https+http://widgetdata-api`, so app-to-app calls are expected to go through Aspire service discovery instead of hard-coded localhost URLs.
- **Most real behavior lives in Infrastructure services.** Controllers are thin; `WidgetService`, `DataSourceService`, `ScheduleService`, `PageService`, `PermissionService`, and related services in `src\WidgetData.Infrastructure\Services` contain the orchestration logic.
- **API startup has side effects.** `src\WidgetData.API\Program.cs` not only configures auth/CORS/rate limiting and middleware, it also resolves `DataSeeder` and runs `SeedAsync()`, which applies EF migrations before seeding demo/admin data.
- **Worker startup also migrates the database.** `src\WidgetData.Worker\Program.cs` resolves `ApplicationDbContext` and calls `Database.MigrateAsync()` before starting `SchedulerWorkerService`.
- **Widget execution is centralized.** `WidgetService.ExecuteAsync()` creates a `WidgetExecution`, optionally archives config for scheduled runs, dispatches to a source-specific loader, and updates execution history plus `Widget.LastExecutedAt` / `LastRowCount`.
- **Integration tests boot the real API startup path.** `tests\WidgetData.IntegrationTests` uses `WebApplicationFactory<Program>`, so those tests exercise the same middleware, configuration, migration, and seeding path as the API itself.

## Key conventions

- **Tenant filtering is ambient and starts before EF.** `TenantContext` is registered before `ApplicationDbContext`; `TenantContextMiddleware` fills it from JWT claim `tenant_id` first, then from `X-Tenant-Id` or `X-Tenant-Slug` headers for anonymous/embed requests.
- **`TenantId == null` means shared data.** Global query filters in `ApplicationDbContext` allow super-admin access, the current tenant, or records with no tenant. Shared widgets, data sources, and groups are intentionally visible across tenants.
- **Bypassing tenant filters is explicit.** Repositories only call `IgnoreQueryFilters()` when they need an exact tenant-targeted query or version lookup; `PageRepository` is the model to follow here.
- **Repository/service naming is uniform.** The codebase consistently uses `IXxxRepository -> XxxRepository` and `IXxxService -> XxxService`, all registered as scoped dependencies in `src\WidgetData.Infrastructure\DependencyInjection.cs`.
- **Widget configuration changes must preserve history.** `WidgetService.UpdateAsync()` automatically creates a `WidgetConfigArchive` whenever `Configuration`, `ChartConfig`, or `HtmlTemplate` changes. Scheduled runs can also archive config when `WidgetSchedule.ArchiveConfigOnRun` is enabled.
- **Widget data loaders all normalize to the same payload shape.** Source-specific methods in `WidgetService` return anonymous objects with `columns` and `rows`; rows are dictionaries keyed by column name for SQLite, CSV, JSON, Excel, and REST API loaders.
- **SQLite widget queries are intentionally read-only.** Before execution, `WidgetService` strips SQL comments, requires the statement to start with `SELECT`, and blocks mutating keywords such as `INSERT`, `UPDATE`, `DELETE`, `DROP`, `CREATE`, `ALTER`, `EXEC`, `TRUNCATE`, `MERGE`, `ATTACH`, and `DETACH`.
- **File-backed data sources are stored under tenant-aware upload paths.** `DataSourceService.UploadFileAsync()` writes files to `uploads\datasources\<tenant-or-shared>\<dataSourceId>\`, updates the metadata fields, and sets the stored path back onto the `DataSource`.
- **Blazor routing expects authentication state to be cascaded.** `src\WidgetData.Web\Components\Routes.razor` wraps the router in `CascadingAuthenticationState`; keep that intact when changing auth-aware UI.

## Important docs

- `README.md`
- `doc\architecture.md`
- `doc\deployment.md`
- `doc\testing.md`
- `doc\security.md`
- `doc\multi-step-processing.md`
- `doc\branching-variables.md`

## Playwright MCP

- Start **`dotnet run --project src\WidgetData.AppHost`** before browser automation when you need the full admin stack; `WidgetData.Web` expects service discovery to reach `widgetdata-api`.
- Default local targets from the docs are:
  - Blazor admin app: `https://localhost:5001`
  - Gateway: `https://localhost:7000`
  - Demo storefront: `http://localhost:3000` after serving `demo\shop\shop-front`
- Use `/health` as the first readiness check before navigating into auth-protected flows.
- Seeded demo credentials live in `src\WidgetData.Infrastructure\Data\Seed\admin\users.json`; the default admin login is `admin@widgetdata.com` / `Admin@123!`.
