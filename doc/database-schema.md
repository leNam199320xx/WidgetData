# Database Schema

## 📋 Tổng quan

Widget Data sử dụng SQL Server làm database chính với Entity Framework Core. Schema gồm **7 bảng chính**:

```
┌─────────────────────────────────────────────────────────────────┐
│                      DATABASE SCHEMA                            │
├──────────────┬──────────────────────────────────────────────────┤
│  AspNetUsers │ Người dùng (ASP.NET Identity)                    │
│  AspNetRoles │ Vai trò (Admin, Manager, Developer, Viewer)      │
├──────────────┴──────────────────────────────────────────────────┤
│  DataSources │ Nguồn dữ liệu (SQL, CSV, Excel, API)             │
│  Widgets     │ Cấu hình widgets (bao gồm steps dạng JSON)       │
├──────────────┴──────────────────────────────────────────────────┤
│  WidgetSchedules   │ Lịch tự động thực thi                      │
│  WidgetExecutions  │ Lịch sử thực thi                           │
│  AuditLogs         │ Nhật ký kiểm toán                          │
└─────────────────────────────────────────────────────────────────┘
```

> 💡 **Ghi chú thiết kế**: Các bước xử lý (steps) của widget được lưu dưới dạng JSON trong cột `Configuration` của bảng `Widgets` thay vì một bảng riêng, giúp đơn giản hóa schema và tăng hiệu năng đọc.

---

## 📐 Entity Relationship Diagram (ERD)

```
AspNetUsers ──── (many-to-many) ──── AspNetRoles
    │ 1
    │─── createdBy ──────────────────────────────┐
    │                                             │
    │ 1                                           │
    │                                             │
    ▼ *                                           │
DataSources                                       │
    │ 1                                           │
    │                                             │
    ▼ *                                           │
Widgets ──── createdBy ──────────────────────────┘
    │ 1      │ (steps stored as JSON in Configuration)
    │        │
    │ 1      │ 1
    ▼ *      ▼ *
WidgetSchedules
    │ 1
    │
    ▼ *
WidgetExecutions
```

---

## 📋 Chi tiết bảng

### 1. AspNetUsers (Người dùng)

```sql
CREATE TABLE AspNetUsers (
    Id                   NVARCHAR(450) NOT NULL PRIMARY KEY,
    Email                NVARCHAR(256) NOT NULL,
    NormalizedEmail      NVARCHAR(256),
    UserName             NVARCHAR(256) NOT NULL,
    NormalizedUserName   NVARCHAR(256),
    DisplayName          NVARCHAR(100),
    PasswordHash         NVARCHAR(MAX),
    SecurityStamp        NVARCHAR(MAX),
    ConcurrencyStamp     NVARCHAR(MAX),
    PhoneNumber          NVARCHAR(MAX),
    TwoFactorEnabled     BIT NOT NULL DEFAULT 0,
    LockoutEnd           DATETIMEOFFSET,
    LockoutEnabled       BIT NOT NULL DEFAULT 1,
    AccessFailedCount    INT NOT NULL DEFAULT 0,
    IsActive             BIT NOT NULL DEFAULT 1,
    CreatedAt            DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastLoginAt          DATETIME2
);

CREATE INDEX IX_AspNetUsers_Email ON AspNetUsers(NormalizedEmail);
```

**Entity (C#):**
```csharp
public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation properties
    public ICollection<Widget> Widgets { get; set; }
    public ICollection<DataSource> DataSources { get; set; }
}
```

---

### 2. AspNetRoles (Vai trò - ASP.NET Identity)

Bảng chuẩn của ASP.NET Core Identity, không cần tạo thủ công. Quản lý qua `RoleManager<IdentityRole>`.

| Vai trò | Quyền hạn |
|---------|-----------|
| `Admin` | Toàn quyền quản trị hệ thống |
| `Manager` | Tạo/sửa/xóa widgets và data sources |
| `Developer` | Tạo/sửa widgets |
| `Viewer` | Chỉ xem và thực thi widgets |

```csharp
// Seed roles khi khởi động ứng dụng
public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Developer = "Developer";
    public const string Viewer = "Viewer";
}
```

---

### 3. DataSources (Nguồn dữ liệu)

```sql
CREATE TABLE DataSources (
    Id                INT IDENTITY(1,1) PRIMARY KEY,
    Name              NVARCHAR(100) NOT NULL,
    SourceType        NVARCHAR(50) NOT NULL,  -- sqlserver|postgresql|mysql|csv|excel|json|restapi
    Description       NVARCHAR(500),
    ConnectionString  NVARCHAR(MAX),          -- AES-256-GCM encrypted
    Host              NVARCHAR(255),
    Port              INT,
    DatabaseName      NVARCHAR(100),
    Username          NVARCHAR(100),
    Password          NVARCHAR(MAX),          -- AES-256-GCM encrypted
    ApiEndpoint       NVARCHAR(500),
    ApiKey            NVARCHAR(MAX),          -- AES-256-GCM encrypted
    AdditionalConfig  NVARCHAR(MAX),          -- JSON for extra settings
    IsActive          BIT NOT NULL DEFAULT 1,
    CreatedBy         NVARCHAR(450) NOT NULL REFERENCES AspNetUsers(Id),
    CreatedAt         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt         DATETIME2,
    LastTestedAt      DATETIME2,
    LastTestResult    NVARCHAR(20)            -- success|failed|unknown
);

CREATE INDEX IX_DataSources_SourceType ON DataSources(SourceType);
CREATE INDEX IX_DataSources_CreatedBy ON DataSources(CreatedBy);
```

**Entity (C#):**
```csharp
public class DataSource
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DataSourceType SourceType { get; set; }
    public string Description { get; set; }
    
    [Encrypted] // Custom attribute - auto encrypt/decrypt
    public string ConnectionString { get; set; }
    public string Host { get; set; }
    public int? Port { get; set; }
    public string DatabaseName { get; set; }
    public string Username { get; set; }
    
    [Encrypted]
    public string Password { get; set; }
    public string ApiEndpoint { get; set; }
    
    [Encrypted]
    public string ApiKey { get; set; }
    public string AdditionalConfig { get; set; }
    
    public bool IsActive { get; set; } = true;
    public string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastTestedAt { get; set; }
    public string LastTestResult { get; set; }
    
    // Navigation
    public ApplicationUser CreatedByUser { get; set; }
    public ICollection<Widget> Widgets { get; set; }
}

public enum DataSourceType
{
    SqlServer, PostgreSql, MySql, SQLite,
    Csv, Excel, Json, RestApi
}
```

---

### 4. Widgets (Widget cấu hình)

```sql
CREATE TABLE Widgets (
    Id                INT IDENTITY(1,1) PRIMARY KEY,
    Name              NVARCHAR(100) NOT NULL,
    WidgetType        NVARCHAR(50) NOT NULL,   -- chart|table|metric|map
    Description       NVARCHAR(500),
    DataSourceId      INT REFERENCES DataSources(Id),
    Configuration     NVARCHAR(MAX) NOT NULL,   -- JSON: steps, variables, parameters
    ChartConfig       NVARCHAR(MAX),            -- JSON: chart appearance settings
    IsActive          BIT NOT NULL DEFAULT 1,
    CacheEnabled      BIT NOT NULL DEFAULT 1,
    CacheTtlMinutes   INT NOT NULL DEFAULT 60,
    LastExecutedAt    DATETIME2,
    LastRowCount      INT,
    CreatedBy         NVARCHAR(450) NOT NULL REFERENCES AspNetUsers(Id),
    CreatedAt         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt         DATETIME2
);

CREATE INDEX IX_Widgets_CreatedBy_IsActive ON Widgets(CreatedBy, IsActive);
CREATE INDEX IX_Widgets_WidgetType ON Widgets(WidgetType);
CREATE INDEX IX_Widgets_DataSourceId ON Widgets(DataSourceId);
```

**Entity (C#):**
```csharp
public class Widget
{
    public int Id { get; set; }
    public string Name { get; set; }
    public WidgetType WidgetType { get; set; }
    public string Description { get; set; }
    public int? DataSourceId { get; set; }
    public string Configuration { get; set; }  // JSON serialized WidgetConfig
    public string ChartConfig { get; set; }
    public bool IsActive { get; set; } = true;
    public bool CacheEnabled { get; set; } = true;
    public int CacheTtlMinutes { get; set; } = 60;
    public DateTime? LastExecutedAt { get; set; }
    public int? LastRowCount { get; set; }
    public string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public DataSource DataSource { get; set; }
    public ApplicationUser CreatedByUser { get; set; }
    public WidgetSchedule Schedule { get; set; }
    public ICollection<WidgetExecution> Executions { get; set; }
}

public enum WidgetType { Chart, Table, Metric, Map }
```

---

### 5. WidgetSchedules (Lịch tự động)

```sql
CREATE TABLE WidgetSchedules (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    WidgetId        INT NOT NULL REFERENCES Widgets(Id) ON DELETE CASCADE,
    CronExpression  NVARCHAR(100) NOT NULL,   -- e.g. "0 2 * * *"
    Timezone        NVARCHAR(50) NOT NULL DEFAULT 'UTC',
    IsEnabled       BIT NOT NULL DEFAULT 1,
    RetryOnFailure  BIT NOT NULL DEFAULT 1,
    MaxRetries      INT NOT NULL DEFAULT 3,
    LastRunAt       DATETIME2,
    LastRunStatus   NVARCHAR(20),             -- success|failed|running
    NextRunAt       DATETIME2,
    HangfireJobId   NVARCHAR(100),
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2
);

-- Partial index for efficient next-run queries
CREATE NONCLUSTERED INDEX IX_WidgetSchedules_NextRun 
    ON WidgetSchedules(IsEnabled, NextRunAt)
    WHERE IsEnabled = 1;
```

---

### 6. WidgetExecutions (Lịch sử thực thi)

```sql
CREATE TABLE WidgetExecutions (
    Id               INT IDENTITY(1,1) PRIMARY KEY,
    ExecutionId      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    WidgetId         INT NOT NULL REFERENCES Widgets(Id),
    ScheduleId       INT REFERENCES WidgetSchedules(Id),
    Status           NVARCHAR(20) NOT NULL,    -- success|failed|running|cancelled
    TriggeredBy      NVARCHAR(50) NOT NULL,    -- manual|scheduler|api
    UserId           NVARCHAR(450) REFERENCES AspNetUsers(Id),
    Parameters       NVARCHAR(MAX),            -- JSON input parameters
    RowCount         INT,
    ExecutionTimeMs  INT,
    ErrorMessage     NVARCHAR(MAX),
    ResultSummary    NVARCHAR(MAX),            -- JSON summary
    StartedAt        DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CompletedAt      DATETIME2
);

CREATE INDEX IX_WidgetExecutions_WidgetId_StartedAt 
    ON WidgetExecutions(WidgetId, StartedAt DESC);
CREATE INDEX IX_WidgetExecutions_Status 
    ON WidgetExecutions(Status);
CREATE INDEX IX_WidgetExecutions_ExecutionId 
    ON WidgetExecutions(ExecutionId);
```

---

### 7. AuditLogs (Nhật ký kiểm toán)

```sql
CREATE TABLE AuditLogs (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    UserId       NVARCHAR(450),
    UserEmail    NVARCHAR(256),
    Action       NVARCHAR(50) NOT NULL,    -- CREATE|UPDATE|DELETE|VIEW|EXECUTE|LOGIN
    EntityType   NVARCHAR(50) NOT NULL,    -- Widget|DataSource|User|Schedule
    EntityId     NVARCHAR(100),
    OldValues    NVARCHAR(MAX),            -- JSON before changes
    NewValues    NVARCHAR(MAX),            -- JSON after changes
    IpAddress    NVARCHAR(50),
    UserAgent    NVARCHAR(500),
    Timestamp    DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Notes        NVARCHAR(500)
);

CREATE INDEX IX_AuditLogs_EntityType_EntityId 
    ON AuditLogs(EntityType, EntityId);
CREATE INDEX IX_AuditLogs_UserId_Timestamp 
    ON AuditLogs(UserId, Timestamp DESC);
CREATE INDEX IX_AuditLogs_Timestamp 
    ON AuditLogs(Timestamp DESC);
```

---

## 🔄 EF Core Migrations

### Tạo migration

```bash
# Thêm migration mới
dotnet ef migrations add InitialCreate \
    --project src/WidgetData.Infrastructure \
    --startup-project src/WidgetData.Web

# Áp dụng migrations
dotnet ef database update \
    --project src/WidgetData.Infrastructure \
    --startup-project src/WidgetData.Web
```

### Seed Data

```csharp
public class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // Seed roles
        var roles = new[] { "Admin", "Manager", "Developer", "Viewer" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
        
        // Seed admin user
        if (!context.Users.Any())
        {
            var admin = new ApplicationUser
            {
                UserName = "admin@widgetdata.com",
                Email = "admin@widgetdata.com",
                DisplayName = "Administrator",
                EmailConfirmed = true
            };
            
            await userManager.CreateAsync(admin, "Admin@123");
            await userManager.AddToRoleAsync(admin, "Admin");
        }
        
        // Seed sample DataSource
        if (!context.DataSources.Any())
        {
            context.DataSources.Add(new DataSource
            {
                Name = "Demo Database",
                SourceType = DataSourceType.SqlServer,
                Description = "Sample demo database",
                Host = "localhost",
                DatabaseName = "WidgetDataDemo"
            });
            await context.SaveChangesAsync();
        }
    }
}
```

---

## 🔐 DbContext & Configuration

```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<DataSource> DataSources { get; set; }
    public DbSet<Widget> Widgets { get; set; }
    public DbSet<WidgetSchedule> WidgetSchedules { get; set; }
    public DbSet<WidgetExecution> WidgetExecutions { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Widget → DataSource
        modelBuilder.Entity<Widget>()
            .HasOne(w => w.DataSource)
            .WithMany(d => d.Widgets)
            .HasForeignKey(w => w.DataSourceId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Widget → Schedule (1:1)
        modelBuilder.Entity<Widget>()
            .HasOne(w => w.Schedule)
            .WithOne(s => s.Widget)
            .HasForeignKey<WidgetSchedule>(s => s.WidgetId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Widget → Executions (1:N)
        modelBuilder.Entity<WidgetExecution>()
            .HasOne(e => e.Widget)
            .WithMany(w => w.Executions)
            .HasForeignKey(e => e.WidgetId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Indexes
        modelBuilder.Entity<Widget>()
            .HasIndex(w => new { w.CreatedBy, w.IsActive });
        
        modelBuilder.Entity<WidgetExecution>()
            .HasIndex(e => new { e.WidgetId, e.StartedAt });
        
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.Timestamp);
        
        // Encrypted fields (via value converter)
        modelBuilder.Entity<DataSource>()
            .Property(d => d.ConnectionString)
            .HasConversion(new EncryptedStringConverter());
        
        modelBuilder.Entity<DataSource>()
            .Property(d => d.Password)
            .HasConversion(new EncryptedStringConverter());
        
        modelBuilder.Entity<DataSource>()
            .Property(d => d.ApiKey)
            .HasConversion(new EncryptedStringConverter());
    }
}
```

---

## 📊 Database Size Estimation

| Bảng | Dự kiến rows (1 năm) | Dung lượng ước tính |
|------|---------------------|---------------------|
| AspNetUsers | 100 - 1,000 | < 1 MB |
| DataSources | 10 - 100 | < 1 MB |
| Widgets | 50 - 500 | ~5 MB |
| WidgetSchedules | 50 - 500 | < 1 MB |
| WidgetExecutions | 50,000 - 500,000 | ~200 MB |
| AuditLogs | 100,000 - 1,000,000 | ~500 MB |
| **TOTAL** | | **~700 MB - 1 GB** |

> 💡 **Lưu ý**: WidgetExecutions và AuditLogs tăng trưởng nhanh. Nên cài đặt **data retention policy** để xóa dữ liệu cũ.

---

## 🧹 Data Retention

```sql
-- Xóa executions cũ hơn 90 ngày
DELETE FROM WidgetExecutions
WHERE StartedAt < DATEADD(day, -90, GETUTCDATE());

-- Xóa audit logs cũ hơn 1 năm
DELETE FROM AuditLogs
WHERE Timestamp < DATEADD(year, -1, GETUTCDATE());
```

```csharp
// Scheduled cleanup job (Hangfire)
RecurringJob.AddOrUpdate<DataCleanupService>(
    "cleanup-old-data",
    service => service.CleanupAsync(),
    Cron.Daily(3)  // Chạy lúc 3 AM mỗi ngày
);
```

---

← [Quay lại INDEX](INDEX.md)
