# Plan tách modules thành projects riêng

## Mục tiêu

Tách từng business module hiện đang nằm trong `WidgetData.Infrastructure` thành các project độc lập, giữ nguyên endpoint contracts và behavior hiện tại.

## Quyết định đã thống nhất

- **Structure**: `src/Modules/[ModuleName]/WidgetData.[ModuleName].csproj`
- **Module policy**: Strict - module chỉ reference `Domain` + `Application`, giao tiếp qua interfaces
- **Thứ tự extract**: Identity → DataSources → Widgets → Delivery → Pages → CrossCutting
- **Xử lý file cũ**: Xóa ngay trong mỗi PR sau khi module đã extract xong
- **Solution folder**: Thêm solution folder `Modules` trong `.sln`
- **Backward compat**: Giữ wrapper `[Obsolete]` tạm trong Infrastructure, xóa ở PR cleanup cuối

## Cấu trúc thư mục cuối cùng

```
src/
  WidgetData.Domain/
  WidgetData.Application/
  WidgetData.Infrastructure/
  WidgetData.API/
  Modules/
    Identity/WidgetData.Identity.csproj
    DataSources/WidgetData.DataSources.csproj
    Widgets/WidgetData.Widgets.csproj
    Delivery/WidgetData.Delivery.csproj
    Pages/WidgetData.Pages.csproj
    CrossCutting/WidgetData.CrossCutting.csproj
```

## Thứ tự PRs (7 PRs)

### PR 1: Tạo module structure + DI registration framework

- Tạo 6 module folders trong `src/Modules/`
- Tạo 6 `.csproj` trống với reference: `Domain` + `Application`
- Tạo 6 module registration interface stubs (`I[Module]Module`)
- Cập nhật `DependencyInjection.cs` để gọi registration methods
- Cập nhật `WidgetData.sln` với solution folder `Modules`
- Deliverable: Build pass, 244 unit tests pass

### PR 2: Extract Identity module

- Move từ `Infrastructure/Modules/Identity/` vào `Modules/Identity/`
- Move `ApplicationUser`, `Tenant`, `UserGroupPermission`, `UserWidgetPermission` entities vào Domain (giữ nguyên namespace)
- Move identity-related repositories, services
- Xóa file cũ trong Infrastructure
- Deliverable: Build pass, tests pass

### PR 3: Extract DataSources module

- Move services: `DataSourceCrudService`, `DataSourceUploadService`, `DataSourceConnectivityTestService`, `DataSourceService`, `IDataSourceStrategy`, `IDataSourceValidator`, validators, strategies
- Move repositories: `DataSourceRepository`, `FileBackedRepositories`, `FileHandler`, `IFileHandler`
- Move DTOs nếu cần (hiện đang ở Application, giữ nguyên)
- Xóa file cũ trong Infrastructure
- Deliverable: Build pass, tests pass

### PR 4: Extract Widgets module

- Move services: `WidgetCrudService`, `WidgetExecutionService`, `WidgetConfigArchiveService`, `WidgetActivityService`, `WidgetGroupService`, `ScheduleService`, `InactivityMonitorService`
- Move repositories: `WidgetRepository`, `ExecutionRepository`, `WidgetConfigArchiveRepository`, `WidgetActivityRepository`, `ScheduleRepository`
- Xóa file cũ trong Infrastructure
- Deliverable: Build pass, tests pass

### PR 5: Extract Delivery module

- Move services: `DeliveryService`, `DeliveryTargetService`, `DeliveryExecutionService`, `DeliveryDispatcher`, channels, strategies
- Move repositories: `DeliveryTargetRepository`, `DeliveryExecutionRepository`
- Xóa file cũ trong Infrastructure
- Deliverable: Build pass, tests pass

### PR 6: Extract Pages module

- Move services: `PageCrudService`, `PageVersioningService`, `PageHtmlService`, `PageLayoutService`, `PageService`, `FormService`, `IdeaBoardService`
- Move repositories: `PageRepository`, `IdeaBoardRepository`
- Xóa file cũ trong Infrastructure
- Deliverable: Build pass, tests pass

### PR 7: Extract CrossCutting + Cleanup

- Move services: `AuditService`, `DashboardService`, `ExportService`, `TenantService`, `PermissionService`, `ExportService`
- Xóa file cũ trong Infrastructure
- Infrastructure giữ lại: `DbContext`, DI wiring, shared utilities, JsonDataProvider
- Deliverable: Build pass, tests pass

## Quy tắc chung cho mỗi PR

1. Tạo module project trong `src/Modules/[Name]/`
2. Move files vào module project
3. Update namespaces nếu cần
4. Xóa file cũ khỏi Infrastructure
5. Cập nhật DI registration
6. Run build + tests
7. Không thay đổi API contracts

## Validation

- Build solution pass sau mỗi PR
- 244 unit tests pass
- Không circular dependency giữa modules
- Integration tests pass (nếu có)

## Rủi ro

- **Circular dependency**: Theo policy Strict, không được phép module reference module khác. Nếu cần, phải thông qua Application interfaces.
- **Tenant filtering**: Các module có cùng `TenantContext` dependency, đã có sẵn.
- **EF DbContext**: Tất cả modules dùng chung `ApplicationDbContext` + `IdentityDbContext`, không cần tách.
- **Backward compat**: Controllers inject interfaces, không cần sửa. Tests cần update references sau cleanup PR.

## Files cần giữ lại trong Infrastructure

Sau khi extract xong 6 modules, Infrastructure chỉ còn:
- `ApplicationDbContext` + `IdentityDbContext`
- `DependencyInjection.cs` (DI wiring)
- `TenantContext`
- `JsonDataProvider`
- `Data/Seed/` (seed data)
- `Startup/` (schema + seed initializers)
