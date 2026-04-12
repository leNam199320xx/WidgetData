# Functions Reference – WidgetData

> Tài liệu này liệt kê toàn bộ chức năng (functions) của WidgetData sau khi cập nhật UI/UX và logic.

---

## Mục lục

1. [Authentication](#1-authentication)
2. [Dashboard](#2-dashboard)
3. [Widgets](#3-widgets)
4. [Widget Configure (Chi tiết widget)](#4-widget-configure)
5. [Data Sources](#5-data-sources)
6. [Widget Groups (Nhóm Widget)](#6-widget-groups)
7. [Schedules](#7-schedules)
8. [Deliveries (Lịch sử gửi)](#8-deliveries)
9. [Users & Permissions](#9-users--permissions)
10. [Settings](#10-settings)
11. [API Service (Web Client)](#11-api-service-web-client)
12. [Application Interfaces (Backend)](#12-application-interfaces-backend)

---

## 1. Authentication

**Page:** `/login`

| Function | Mô tả |
|----------|-------|
| `OnInitializedAsync()` | Kiểm tra nếu đã đăng nhập thì chuyển về `/` |
| `Login()` | Gọi `ApiService.LoginAsync`, lưu token, điều hướng về trang chủ |

**API:** `POST /api/auth/login`

---

## 2. Dashboard

**Page:** `/` (Home)

| Function | Mô tả |
|----------|-------|
| `OnInitializedAsync()` | Tải thống kê dashboard qua `ApiService.GetDashboardStatsAsync()` |

**Dữ liệu hiển thị:**
- Tổng số widgets (`TotalWidgets`)
- Tổng số data sources (`TotalDataSources`)
- Số lịch chạy đang active (`ActiveSchedules`)
- Số lần chạy thành công (`SuccessfulExecutions`)
- Bảng `RecentExecutions`: tên widget, trạng thái, thời gian bắt đầu, thời lượng (ms)

---

## 3. Widgets

**Page:** `/widgets`

| Function | Mô tả |
|----------|-------|
| `OnInitializedAsync()` | Gọi `LoadAsync()` |
| `LoadAsync()` | Tải danh sách widgets và data sources |
| `OpenAddForm()` | Mở form thêm widget mới (reset form) |
| `OpenEditForm(WidgetDto w)` | Mở form chỉnh sửa, điền sẵn dữ liệu của widget đã chọn |
| `CancelForm()` | Đóng form, reset `_editingId` |
| `SaveWidget()` | Tạo mới (nếu `_editingId == null`) hoặc cập nhật widget; hiển thị snackbar |
| `ExecuteWidget(WidgetDto w)` | Chạy widget ngay lập tức, hiển thị kết quả (status, ms, số dòng) |
| `ViewHistory(WidgetDto w)` | Hiển thị lịch sử thực thi của widget |
| `DoDelete()` | Xác nhận và xóa widget đã chọn |
| `StatusColor(ExecutionStatus s)` | Trả về màu MudBlazor tương ứng với trạng thái thực thi |

**Fields trong form:**
- `Name` – tên kỹ thuật
- `FriendlyLabel` – tên hiển thị thân thiện
- `HelpText` – mô tả ngắn (hiển thị tooltip)
- `WidgetType` (Table / Chart / v.v.)
- `DataSourceId`
- `Description`
- `Configuration` (JSON)
- `ChartConfig` (JSON – chỉ hiện khi type = Chart)
- `CacheEnabled`, `CacheTtlMinutes`
- `IsActive` (chỉ khi edit)

---

## 4. Widget Configure

**Page:** `/widgets/configure/{id}`

| Function | Mô tả |
|----------|-------|
| `OnInitializedAsync()` | Tải widget, data sources, groups, schedules, delivery targets song song |
| `LoadWidgetAsync()` | Lấy chi tiết widget theo `Id` |
| `LoadDataSourcesAsync()` | Lấy danh sách data sources |
| `LoadGroupsAsync()` | Lấy danh sách widget groups |
| `LoadSchedulesAsync()` | Lấy lịch chạy, lọc theo widget hiện tại |
| `LoadDeliveryTargetsAsync()` | Lấy danh sách delivery targets của widget |
| `SaveWidget()` | Lưu thay đổi cấu hình widget (UpdateWidgetDto) |
| `DownloadExport(string format)` | Xuất dữ liệu widget ra file (CSV / Excel / JSON / PDF) |
| `TriggerDelivery(DeliveryTargetDto target)` | Gửi kết quả widget đến một delivery target ngay lập tức |
| `DeleteDeliveryTarget(int targetId)` | Xóa một delivery target |
| `OpenAddDeliveryForm()` | Mở form thêm delivery target mới |
| `OnDeliveryTypeChanged(DeliveryType t)` | Reset form delivery khi đổi loại |
| `SaveDeliveryTarget()` | Tạo mới hoặc cập nhật delivery target |

**Export formats:** `csv`, `excel`, `json`, `pdf`

---

## 5. Data Sources

**Page:** `/datasources`

| Function | Mô tả |
|----------|-------|
| `OnInitializedAsync()` | Gọi `LoadAsync()` |
| `LoadAsync()` | Tải danh sách data sources |
| `OpenAddForm()` | Mở form thêm data source mới |
| `OpenEditForm(DataSourceDto ds)` | Mở form chỉnh sửa, điền sẵn dữ liệu |
| `CancelForm()` | Đóng form |
| `SaveDataSource()` | Tạo mới hoặc cập nhật data source |
| `TestConnection(DataSourceDto ds)` | Kiểm tra kết nối data source, hiển thị kết quả |
| `DoDelete()` | Xác nhận và xóa data source đã chọn |

**Fields (theo loại):**
- **SQLite:** `ConnectionString`
- **RestApi:** `ApiEndpoint`, `ApiKey`
- **SqlServer / PostgreSql / MySql:** `Host`, `Port`, `DatabaseName`, `Username`, `Password`
- **Khác:** `AdditionalConfig` (JSON)

**DataSourceType:** `SQLite`, `RestApi`, `SqlServer`, `PostgreSql`, `MySql`

---

## 6. Widget Groups

**Page:** `/widget-groups`

| Function | Mô tả |
|----------|-------|
| `OnInitializedAsync()` | Tải groups và widgets song song |
| `LoadGroupsAsync()` | Lấy danh sách widget groups |
| `LoadWidgetsAsync()` | Lấy danh sách widgets (để chọn vào nhóm) |
| `OpenAddForm()` | Mở form tạo nhóm mới |
| `OpenEditForm(WidgetGroupDto g)` | Mở form chỉnh sửa nhóm, điền sẵn dữ liệu + widget đã chọn |
| `CancelForm()` | Đóng form |
| `Save()` | Tạo mới hoặc cập nhật nhóm (bao gồm danh sách widget IDs) |
| `DoDelete()` | Xác nhận và xóa nhóm đã chọn |

**Fields:**
- `Name` – tên nhóm
- `Description` – mô tả
- `WidgetIds` – danh sách widgets trong nhóm (chọn qua MudChipSet)
- `IsActive` (chỉ khi edit)

---

## 7. Schedules

**Page:** `/schedules`

| Function | Mô tả |
|----------|-------|
| `OnInitializedAsync()` | Gọi `LoadAsync()` |
| `LoadAsync()` | Tải schedules và widgets |
| `OpenAddForm()` | Mở form thêm schedule mới (mặc định: UTC, enabled, maxRetries=3) |
| `OpenEditForm(WidgetScheduleDto s)` | Mở form chỉnh sửa schedule |
| `CancelForm()` | Đóng form |
| `SaveSchedule()` | Tạo mới hoặc cập nhật schedule |
| `ToggleSchedule(WidgetScheduleDto s, bool enable)` | Bật / tắt schedule |
| `DoDelete()` | Xác nhận và xóa schedule đã chọn |
| `StatusColor(ExecutionStatus s)` | Trả về màu tương ứng trạng thái |

**Fields:**
- `WidgetId` – widget áp dụng
- `CronExpression` – biểu thức cron (VD: `0 6 * * *`)
- `Timezone` – múi giờ
- `IsEnabled`, `RetryOnFailure`, `MaxRetries`

**Cột hiển thị:** Widget, Cron, Timezone, Enabled, Last Run (+ status), Next Run

---

## 8. Deliveries

**Page:** `/deliveries`

| Function | Mô tả |
|----------|-------|
| `OnInitializedAsync()` | Tải widgets, rồi tải lịch sử gửi |
| `OnWidgetChanged(int widgetId)` | Đổi widget được chọn, tải lại executions |
| `LoadExecutionsAsync()` | Tải lịch sử gửi: nếu chọn widget cụ thể thì lấy riêng, ngược lại gộp tất cả |
| `GetWidgetName(int id)` | Trả về tên hiển thị của widget theo ID |
| `GetStatusColor(ExecutionStatus s)` | Màu chip theo trạng thái |
| `GetStatusLabel(ExecutionStatus s)` | Nhãn tiếng Việt: ✅ Thành công / ❌ Thất bại / ⏳ Đang chạy |

**Cột hiển thị:** Widget, Điểm gửi, Trạng thái, Thời gian, Thông báo, Người gửi

---

## 9. Users & Permissions

**Page:** `/users`

### Tab 1 – Danh sách người dùng

| Function | Mô tả |
|----------|-------|
| `OnInitializedAsync()` | Tải users và widgets |
| `LoadUsersAsync()` | Lấy danh sách users |
| `SaveUser()` | Tạo user mới (email, display name, password) |
| `ShowPermissions(UserDto u)` | Chọn user, tải permissions để quản lý |

**Cột hiển thị:** Email, Display Name, Roles, Status, Last Login, nút "Phân quyền"

### Tab 2 – Phân quyền Widget

| Function | Mô tả |
|----------|-------|
| `ShowPermissions(UserDto u)` | Tải `UserPermissionDto` của user được chọn |
| `AssignWidgetPerm()` | Gán quyền widget cho user (CanView, CanExecute, CanEdit) |
| `RemovePerm(UserPermissionDto p)` | Xóa quyền widget |

**Permission flags:** `CanView`, `CanExecute`, `CanEdit`

---

## 10. Settings

**Page:** `/settings`

> Trang placeholder – chưa có chức năng cụ thể (dự kiến cấu hình tùy chọn ứng dụng).

---

## 11. API Service (Web Client)

Lớp `ApiService` (Blazor Web) đóng gói toàn bộ HTTP calls đến backend API.

### Auth
| Method | HTTP | Endpoint |
|--------|------|----------|
| `LoginAsync(LoginDto)` | POST | `/api/auth/login` |

### Dashboard
| Method | HTTP | Endpoint |
|--------|------|----------|
| `GetDashboardStatsAsync()` | GET | `/api/dashboard/stats` |

### Data Sources
| Method | HTTP | Endpoint |
|--------|------|----------|
| `GetDataSourcesAsync()` | GET | `/api/datasources` |
| `CreateDataSourceAsync(dto)` | POST | `/api/datasources` |
| `UpdateDataSourceAsync(id, dto)` | PUT | `/api/datasources/{id}` |
| `DeleteDataSourceAsync(id)` | DELETE | `/api/datasources/{id}` |
| `TestDataSourceAsync(id)` | POST | `/api/datasources/{id}/test` |

### Widgets
| Method | HTTP | Endpoint |
|--------|------|----------|
| `GetWidgetsAsync()` | GET | `/api/widgets` |
| `GetWidgetByIdAsync(id)` | GET | `/api/widgets/{id}` |
| `CreateWidgetAsync(dto)` | POST | `/api/widgets` |
| `UpdateWidgetAsync(id, dto)` | PUT | `/api/widgets/{id}` |
| `DeleteWidgetAsync(id)` | DELETE | `/api/widgets/{id}` |
| `ExecuteWidgetAsync(id)` | POST | `/api/widgets/{id}/execute` |
| `GetWidgetHistoryAsync(id)` | GET | `/api/widgets/{id}/history` |
| `ExportWidgetAsync(id, format)` | GET | `/api/widgets/{id}/export?format=` |
| `GetExportUrl(id, format)` | — | URL helper |
| `TriggerDeliveryAsync(widgetId, targetId)` | POST | `/api/widgets/{widgetId}/deliver/{targetId}` |
| `GetDeliveryExecutionsAsync(widgetId)` | GET | `/api/widgets/{widgetId}/deliveries` |

### Schedules
| Method | HTTP | Endpoint |
|--------|------|----------|
| `GetSchedulesAsync()` | GET | `/api/schedules` |
| `CreateScheduleAsync(dto)` | POST | `/api/schedules` |
| `UpdateScheduleAsync(id, dto)` | PUT | `/api/schedules/{id}` |
| `DeleteScheduleAsync(id)` | DELETE | `/api/schedules/{id}` |
| `EnableScheduleAsync(id)` | POST | `/api/schedules/{id}/enable` |
| `DisableScheduleAsync(id)` | POST | `/api/schedules/{id}/disable` |

### Users
| Method | HTTP | Endpoint |
|--------|------|----------|
| `GetUsersAsync()` | GET | `/api/users` |
| `CreateUserAsync(RegisterDto)` | POST | `/api/users` |

### Widget Groups
| Method | HTTP | Endpoint |
|--------|------|----------|
| `GetWidgetGroupsAsync()` | GET | `/api/widget-groups` |
| `GetWidgetGroupAsync(id)` | GET | `/api/widget-groups/{id}` |
| `CreateWidgetGroupAsync(dto)` | POST | `/api/widget-groups` |
| `UpdateWidgetGroupAsync(id, dto)` | PUT | `/api/widget-groups/{id}` |
| `DeleteWidgetGroupAsync(id)` | DELETE | `/api/widget-groups/{id}` |

### Permissions
| Method | HTTP | Endpoint |
|--------|------|----------|
| `GetUserPermissionsAsync(userId)` | GET | `/api/permissions/user/{userId}` |
| `GetWidgetPermissionsAsync(widgetId)` | GET | `/api/permissions/widget/{widgetId}` |
| `GetGroupPermissionsAsync(groupId)` | GET | `/api/permissions/group/{groupId}` |
| `AssignWidgetPermissionAsync(dto)` | POST | `/api/permissions/widget` |
| `AssignGroupPermissionAsync(dto)` | POST | `/api/permissions/group` |
| `RemoveWidgetPermissionAsync(permissionId)` | DELETE | `/api/permissions/widget/{permissionId}` |
| `RemoveGroupPermissionAsync(permissionId)` | DELETE | `/api/permissions/group/{permissionId}` |

### Delivery Targets
| Method | HTTP | Endpoint |
|--------|------|----------|
| `GetDeliveryTargetsAsync(widgetId)` | GET | `/api/delivery-targets/widget/{widgetId}` |
| `CreateDeliveryTargetAsync(dto)` | POST | `/api/delivery-targets` |
| `UpdateDeliveryTargetAsync(id, dto)` | PUT | `/api/delivery-targets/{id}` |
| `DeleteDeliveryTargetAsync(id)` | DELETE | `/api/delivery-targets/{id}` |

---

## 12. Application Interfaces (Backend)

### IWidgetService
| Method | Mô tả |
|--------|-------|
| `GetAllAsync()` | Lấy tất cả widgets |
| `GetByIdAsync(id)` | Lấy widget theo ID |
| `CreateAsync(dto, userId)` | Tạo widget mới |
| `UpdateAsync(id, dto)` | Cập nhật widget |
| `DeleteAsync(id)` | Xóa widget |
| `ExecuteAsync(id, userId)` | Chạy widget, trả về `WidgetExecutionDto` |
| `GetDataAsync(id)` | Lấy dữ liệu kết quả của widget |
| `GetHistoryAsync(id)` | Lấy lịch sử thực thi |

### IDataSourceService
| Method | Mô tả |
|--------|-------|
| `GetAllAsync()` | Lấy tất cả data sources |
| `GetByIdAsync(id)` | Lấy data source theo ID |
| `CreateAsync(dto, userId)` | Tạo data source mới |
| `UpdateAsync(id, dto)` | Cập nhật data source |
| `DeleteAsync(id)` | Xóa data source |
| `TestConnectionAsync(id)` | Kiểm tra kết nối, trả về chuỗi thông báo |

### IScheduleService
| Method | Mô tả |
|--------|-------|
| `GetAllAsync()` | Lấy tất cả schedules |
| `CreateAsync(dto)` | Tạo schedule mới |
| `UpdateAsync(id, dto)` | Cập nhật schedule |
| `DeleteAsync(id)` | Xóa schedule |
| `EnableAsync(id)` | Bật schedule |
| `DisableAsync(id)` | Tắt schedule |

### IWidgetGroupService
| Method | Mô tả |
|--------|-------|
| `GetAllAsync()` | Lấy tất cả widget groups |
| `GetByIdAsync(id)` | Lấy group theo ID |
| `CreateAsync(dto, userId)` | Tạo group mới |
| `UpdateAsync(id, dto)` | Cập nhật group |
| `DeleteAsync(id)` | Xóa group |

### IDeliveryService
| Method | Mô tả |
|--------|-------|
| `GetTargetsAsync(widgetId)` | Lấy delivery targets của widget |
| `GetTargetByIdAsync(id)` | Lấy delivery target theo ID |
| `CreateTargetAsync(dto, userId)` | Tạo delivery target mới |
| `UpdateTargetAsync(id, dto)` | Cập nhật delivery target |
| `DeleteTargetAsync(id)` | Xóa delivery target |
| `DeliverAsync(widgetId, targetId, userId)` | Gửi kết quả widget tới target |
| `GetExecutionsAsync(widgetId)` | Lấy lịch sử gửi của widget |

### IPermissionService
| Method | Mô tả |
|--------|-------|
| `HasWidgetAccessAsync(userId, widgetId, action)` | Kiểm tra quyền truy cập widget |
| `GetAccessibleWidgetIdsAsync(userId)` | Lấy danh sách widget IDs mà user được phép truy cập |
| `GetWidgetPermissionsAsync(widgetId)` | Lấy tất cả quyền của một widget |
| `GetGroupPermissionsAsync(groupId)` | Lấy tất cả quyền của một group |
| `GetUserPermissionsAsync(userId)` | Lấy tất cả quyền của một user |
| `AssignWidgetPermissionAsync(dto)` | Gán quyền widget cho user |
| `AssignGroupPermissionAsync(dto)` | Gán quyền group cho user |
| `RemoveWidgetPermissionAsync(permissionId)` | Thu hồi quyền widget |
| `RemoveGroupPermissionAsync(permissionId)` | Thu hồi quyền group |

### IExportService
| Method | Mô tả |
|--------|-------|
| `ExportAsync(widgetId, format)` | Xuất dữ liệu widget ra file byte[] |
| `GetContentType(format)` | Trả về MIME type tương ứng format |
| `GetFileName(widgetId, format)` | Trả về tên file xuất |

### IDashboardService
| Method | Mô tả |
|--------|-------|
| `GetStatsAsync()` | Lấy thống kê tổng quan dashboard |

### IAuditService
| Method | Mô tả |
|--------|-------|
| `LogAsync(action, entityType, entityId, oldValues, newValues, userId, userEmail, ipAddress, userAgent, notes)` | Ghi audit log cho mọi hành động quan trọng |

---

## Navigation

| Route | Tên trang |
|-------|-----------|
| `/` | Dashboard |
| `/widgets` | Widgets |
| `/widgets/configure/{id}` | Widget Configure |
| `/widget-groups` | Nhóm Widget |
| `/datasources` | Data Sources |
| `/schedules` | Schedules |
| `/deliveries` | Lịch sử gửi |
| `/users` | Users & Permissions |
| `/settings` | Settings |
| `/login` | Đăng nhập |
