# API Reference

## 📋 Tổng quan

Widget Data cung cấp REST API đầy đủ để quản lý và thực thi widgets, data sources, schedules và người dùng.

- **Base URL**: `https://localhost:7001/api`
- **Format**: JSON (application/json)
- **Auth**: JWT Bearer Token
- **Swagger UI**: `https://localhost:7001/swagger`

---

## 🔐 Authentication

### Đăng nhập

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@widgetdata.com",
  "password": "Admin@123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
  "expiresAt": "2026-04-10T11:16:46Z",
  "user": {
    "id": "user-123",
    "email": "admin@widgetdata.com",
    "roles": ["Admin"]
  }
}
```

### Sử dụng Token

```http
GET /api/widgets
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Làm mới Token

```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4..."
}
```

---

## 📦 Widgets API

### Lấy danh sách Widgets

```http
GET /api/widgets
Authorization: Bearer {token}
```

**Query Parameters:**

| Parameter | Type | Mô tả |
|-----------|------|-------|
| `page` | int | Trang hiện tại (mặc định: 1) |
| `pageSize` | int | Số lượng mỗi trang (mặc định: 20, tối đa: 100) |
| `type` | string | Lọc theo loại: `chart`, `table`, `metric`, `map` |
| `isActive` | bool | Lọc theo trạng thái |
| `search` | string | Tìm theo tên |
| `orderBy` | string | Sắp xếp: `name`, `createdAt`, `updatedAt` |
| `orderDir` | string | Hướng: `asc`, `desc` |

**Response:**
```json
{
  "data": [
    {
      "id": 1,
      "name": "Monthly Revenue",
      "widgetType": "chart",
      "description": "Doanh thu tháng",
      "isActive": true,
      "lastExecutedAt": "2026-04-10T09:00:00Z",
      "createdAt": "2026-01-01T00:00:00Z",
      "updatedAt": "2026-04-10T08:00:00Z",
      "dataSource": {
        "id": 5,
        "name": "Production DB",
        "type": "sqlserver"
      }
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 45,
    "totalPages": 3
  }
}
```

---

### Lấy chi tiết Widget

```http
GET /api/widgets/{id}
Authorization: Bearer {token}
```

**Response:**
```json
{
  "id": 1,
  "name": "Monthly Revenue",
  "widgetType": "chart",
  "description": "Doanh thu theo tháng",
  "isActive": true,
  "configuration": {
    "steps": [...],
    "variables": {...},
    "parameters": [...]
  },
  "schedule": {
    "id": 10,
    "cron": "0 2 * * *",
    "timezone": "Asia/Ho_Chi_Minh",
    "isEnabled": true,
    "nextRunAt": "2026-04-11T02:00:00Z"
  },
  "cache": {
    "enabled": true,
    "ttlMinutes": 60,
    "lastCachedAt": "2026-04-10T09:00:00Z"
  },
  "stats": {
    "totalExecutions": 120,
    "successRate": 98.5,
    "avgExecutionTimeMs": 342
  },
  "createdBy": "admin@widgetdata.com",
  "createdAt": "2026-01-01T00:00:00Z"
}
```

---

### Tạo Widget mới

```http
POST /api/widgets
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "name": "Sales Dashboard",
  "widgetType": "chart",
  "description": "Biểu đồ doanh thu bán hàng",
  "dataSourceId": 5,
  "configuration": {
    "steps": [
      {
        "stepId": 1,
        "stepName": "Extract Sales",
        "stepType": "extract",
        "source": {
          "type": "database",
          "query": "SELECT sale_date, amount FROM sales WHERE sale_date >= '${start_date}'"
        },
        "output": "sales_data"
      },
      {
        "stepId": 2,
        "stepName": "Aggregate by Month",
        "stepType": "aggregate",
        "aggregateConfig": {
          "groupBy": ["MONTH(sale_date)"],
          "aggregations": [
            { "column": "amount", "function": "sum", "alias": "monthly_revenue" }
          ]
        },
        "input": "sales_data",
        "output": "monthly_totals"
      }
    ],
    "parameters": [
      {
        "name": "start_date",
        "type": "date",
        "required": true,
        "default": "2026-01-01"
      }
    ]
  },
  "schedule": {
    "enabled": true,
    "cron": "0 0 * * *",
    "timezone": "Asia/Ho_Chi_Minh"
  }
}
```

**Response:** `201 Created`
```json
{
  "id": 42,
  "name": "Sales Dashboard",
  "widgetType": "chart",
  "createdAt": "2026-04-10T09:16:46Z"
}
```

---

### Cập nhật Widget

```http
PUT /api/widgets/{id}
Authorization: Bearer {token}
Content-Type: application/json
```

Request body giống như tạo mới. **Response:** `200 OK`

---

### Xóa Widget

```http
DELETE /api/widgets/{id}
Authorization: Bearer {token}
```

**Response:** `204 No Content`

---

### Thực thi Widget

```http
POST /api/widgets/{id}/execute
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "parameters": {
    "start_date": "2026-01-01",
    "end_date": "2026-12-31",
    "category": "Electronics"
  },
  "forceRefresh": false
}
```

**Response:**
```json
{
  "executionId": "exec-789",
  "widgetId": 1,
  "status": "success",
  "data": [
    { "month": 1, "monthly_revenue": 150000 },
    { "month": 2, "monthly_revenue": 175000 }
  ],
  "columns": [
    { "name": "month", "type": "int" },
    { "name": "monthly_revenue", "type": "decimal" }
  ],
  "rowCount": 12,
  "executionTimeMs": 342,
  "cached": false,
  "executedAt": "2026-04-10T09:16:46Z"
}
```

---

### Lấy dữ liệu Widget (từ cache)

```http
GET /api/widgets/{id}/data
Authorization: Bearer {token}
```

**Query Parameters:**

| Parameter | Type | Mô tả |
|-----------|------|-------|
| `page` | int | Trang (cho dữ liệu lớn) |
| `pageSize` | int | Số dòng mỗi trang |
| `format` | string | `json` (mặc định), `csv`, `excel` |

---

### Lịch sử thực thi Widget

```http
GET /api/widgets/{id}/history
Authorization: Bearer {token}
```

**Query Parameters:** `page`, `pageSize`, `from`, `to`, `status`

**Response:**
```json
{
  "data": [
    {
      "executionId": "exec-789",
      "status": "success",
      "rowCount": 12,
      "executionTimeMs": 342,
      "executedAt": "2026-04-10T09:16:46Z",
      "executedBy": "scheduler"
    }
  ],
  "pagination": { "page": 1, "pageSize": 20, "totalCount": 120, "totalPages": 6 }
}
```

---

## 🗄️ Data Sources API

### Lấy danh sách Data Sources

```http
GET /api/datasources
Authorization: Bearer {token}
```

**Response:**
```json
{
  "data": [
    {
      "id": 5,
      "name": "Production DB",
      "type": "sqlserver",
      "host": "prod-server.database.windows.net",
      "database": "MainDB",
      "isActive": true,
      "lastTestedAt": "2026-04-10T08:00:00Z",
      "lastTestResult": "success"
    }
  ]
}
```

---

### Tạo Data Source

```http
POST /api/datasources
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "name": "Production Database",
  "type": "sqlserver",
  "connectionString": "Server=prod-server;Database=MainDB;...",
  "description": "Database sản xuất chính"
}
```

> ⚠️ Connection string được mã hóa AES-256-GCM trước khi lưu.

**Các type hỗ trợ:** `sqlserver`, `postgresql`, `mysql`, `sqlite`, `csv`, `excel`, `json`, `restapi`

---

### Test kết nối Data Source

```http
POST /api/datasources/{id}/test
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "message": "Connection successful",
  "latencyMs": 45,
  "testedAt": "2026-04-10T09:16:46Z"
}
```

---

## 📅 Schedules API

### Lấy danh sách Schedules

```http
GET /api/schedules
Authorization: Bearer {token}
```

**Response:**
```json
{
  "data": [
    {
      "id": 10,
      "widgetId": 1,
      "widgetName": "Monthly Revenue",
      "cron": "0 2 * * *",
      "cronDescription": "Mỗi ngày lúc 2:00 AM",
      "timezone": "Asia/Ho_Chi_Minh",
      "isEnabled": true,
      "lastRunAt": "2026-04-10T02:00:00Z",
      "nextRunAt": "2026-04-11T02:00:00Z",
      "lastRunStatus": "success"
    }
  ]
}
```

---

### Tạo Schedule

```http
POST /api/schedules
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "widgetId": 1,
  "cron": "*/5 * * * *",
  "timezone": "Asia/Ho_Chi_Minh",
  "isEnabled": true,
  "retryOnFailure": true,
  "maxRetries": 3
}
```

**Cron examples:**
- `*/5 * * * *` → Mỗi 5 phút
- `0 * * * *` → Mỗi giờ
- `0 2 * * *` → Mỗi ngày lúc 2:00 AM
- `0 9 * * 1` → Thứ Hai lúc 9:00 AM
- `0 0 1 * *` → Đầu tháng

---

### Bật/Tắt Schedule

```http
POST /api/schedules/{id}/enable
POST /api/schedules/{id}/disable
Authorization: Bearer {token}
```

**Response:** `200 OK`

---

## 👥 Users API

### Lấy danh sách Users (Admin only)

```http
GET /api/users
Authorization: Bearer {token}
```

**Response:**
```json
{
  "data": [
    {
      "id": "user-123",
      "email": "admin@widgetdata.com",
      "displayName": "Administrator",
      "roles": ["Admin"],
      "isActive": true,
      "lastLoginAt": "2026-04-10T09:00:00Z",
      "createdAt": "2026-01-01T00:00:00Z"
    }
  ]
}
```

---

### Tạo User

```http
POST /api/users
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "email": "developer@company.com",
  "displayName": "Dev User",
  "password": "SecurePass@123",
  "roles": ["Developer"]
}
```

**Roles:** `Admin`, `Manager`, `Developer`, `Viewer`

---

## 📊 Dashboard API

### Lấy thống kê Dashboard

```http
GET /api/dashboard/stats
Authorization: Bearer {token}
```

**Response:**
```json
{
  "totalWidgets": 45,
  "activeWidgets": 38,
  "totalExecutionsToday": 156,
  "failedExecutionsToday": 3,
  "avgExecutionTimeMs": 287,
  "cacheHitRate": 85.2,
  "topWidgets": [
    {
      "id": 1,
      "name": "Monthly Revenue",
      "executionCount": 24
    }
  ],
  "recentExecutions": [...]
}
```

---

## ❌ Error Responses

Tất cả lỗi trả về theo format chuẩn:

```json
{
  "error": {
    "code": "WIDGET_NOT_FOUND",
    "message": "Widget with ID 999 not found",
    "details": null,
    "timestamp": "2026-04-10T09:16:46Z",
    "traceId": "trace-abc123"
  }
}
```

### HTTP Status Codes

| Code | Ý nghĩa |
|------|---------|
| `200` | OK - Thành công |
| `201` | Created - Tạo mới thành công |
| `204` | No Content - Xóa thành công |
| `400` | Bad Request - Dữ liệu đầu vào không hợp lệ |
| `401` | Unauthorized - Chưa đăng nhập / Token hết hạn |
| `403` | Forbidden - Không có quyền truy cập |
| `404` | Not Found - Không tìm thấy tài nguyên |
| `429` | Too Many Requests - Vượt rate limit |
| `500` | Internal Server Error - Lỗi server |

### Error Codes

| Code | Mô tả |
|------|-------|
| `WIDGET_NOT_FOUND` | Không tìm thấy widget |
| `DATASOURCE_NOT_FOUND` | Không tìm thấy data source |
| `DATASOURCE_CONNECTION_FAILED` | Kết nối data source thất bại |
| `WIDGET_EXECUTION_FAILED` | Thực thi widget thất bại |
| `VALIDATION_ERROR` | Dữ liệu không hợp lệ |
| `UNAUTHORIZED` | Chưa xác thực |
| `FORBIDDEN` | Không có quyền |
| `RATE_LIMIT_EXCEEDED` | Vượt quá giới hạn request |

---

## 🔒 Rate Limiting

| Endpoint | Giới hạn |
|----------|---------|
| `POST /api/auth/login` | 10 req/phút/IP |
| `POST /api/widgets/{id}/execute` | 100 req/giờ/user |
| Tất cả endpoints | 60 req/phút/IP |

Headers trả về khi gần đến giới hạn:
```http
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 5
X-RateLimit-Reset: 1712740606
```

---

## 📄 Pagination

Tất cả danh sách hỗ trợ pagination:

```http
GET /api/widgets?page=2&pageSize=20
```

Response luôn bao gồm:
```json
{
  "data": [...],
  "pagination": {
    "page": 2,
    "pageSize": 20,
    "totalCount": 85,
    "totalPages": 5,
    "hasNextPage": true,
    "hasPreviousPage": true
  }
}
```

---

## 🔍 Filtering & Sorting

```http
# Lọc theo loại và trạng thái
GET /api/widgets?type=chart&isActive=true

# Tìm kiếm theo tên
GET /api/widgets?search=revenue

# Sắp xếp
GET /api/widgets?orderBy=createdAt&orderDir=desc

# Kết hợp
GET /api/widgets?type=chart&isActive=true&search=sales&orderBy=name&orderDir=asc&page=1&pageSize=10
```

---

## 📋 Export API

### Export dữ liệu Widget

```http
GET /api/widgets/{id}/data?format=csv
GET /api/widgets/{id}/data?format=excel
GET /api/widgets/{id}/data?format=json
Authorization: Bearer {token}
```

**Response Headers (CSV/Excel):**
```http
Content-Type: text/csv
Content-Disposition: attachment; filename="widget_1_2026-04-10.csv"
```

---

## 🔗 Swagger / OpenAPI

Truy cập Swagger UI tại:
- **Development**: `https://localhost:7001/swagger`
- **Production**: Tắt theo mặc định (bật qua config)

```csharp
// Để bật Swagger trong production:
// appsettings.Production.json
{
  "Swagger": {
    "Enabled": true,
    "RequireAuth": true
  }
}
```

---

← [Quay lại INDEX](INDEX.md)
