# Multi-Tenancy Architecture

## 🎯 KHUYẾN NGHỊ: Shared Database + TenantId (Đơn giản nhất)

**Giải pháp đơn giản nhất để bắt đầu:**
- ✅ Thêm column `TenantId` vào mỗi bảng
- ✅ Global Query Filter tự động lọc data
- ✅ 1 database, 1 application instance
- ✅ Chi phí thấp, dễ maintain
- ⚡ **Implement trong 1-2 ngày**

[Xem Quick Start Guide](#-quick-start-shared-database--tenantid) để triển khai ngay!

---

## ⚠️ QUAN TRỌNG: Quyết định Kiến trúc

**CÓ NÊN tính toán trước?** → **TUYỆT ĐỐI CÓ!** ✅

Multi-tenancy là quyết định kiến trúc **CỰC KỲ KHÓ thay đổi sau này**. Nếu bạn dự định:
- Bán cho nhiều công ty (SaaS model)
- Clone project cho từng khách hàng
- Mở rộng ra nhiều chi nhánh/đơn vị

→ **BẮT BUỘC phải thiết kế multi-tenancy từ NGAY BÂY GIỜ!**

---

## 📋 Tổng quan Multi-Tenancy

### Single-Tenant vs Multi-Tenant

```
┌─────────────────────────────────────────────────────────┐
│ SINGLE-TENANT (Mỗi công ty 1 instance riêng)            │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Company A          Company B          Company C        │
│  ┌──────────┐      ┌──────────┐      ┌──────────┐      │
│  │ App      │      │ App      │      │ App      │      │
│  │ Instance │      │ Instance │      │ Instance │      │
│  └────┬─────┘      └────┬─────┘      └────┬─────┘      │
│       │                 │                 │             │
│  ┌────▼─────┐      ┌────▼─────┐      ┌────▼─────┐      │
│  │ Database │      │ Database │      │ Database │      │
│  │ A        │      │ B        │      │ C        │      │
│  └──────────┘      └──────────┘      └──────────┘      │
│                                                         │
│  ✅ Data isolation tuyệt đối                            │
│  ✅ Customize per tenant dễ dàng                        │
│  ❌ Chi phí cao (N servers, N databases)                │
│  ❌ Khó maintain (update N instances)                   │
│  ❌ Không scale hiệu quả                                │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ MULTI-TENANT (1 instance cho tất cả)                    │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Company A + Company B + Company C                      │
│  ┌──────────────────────────┐                          │
│  │   Shared App Instance    │                          │
│  │   (Multi-Tenant)         │                          │
│  └────────────┬─────────────┘                          │
│               │                                         │
│  ┌────────────▼─────────────┐                          │
│  │  Shared Database          │                          │
│  │  ┌─────────────────────┐ │                          │
│  │  │ Tenant A Data       │ │                          │
│  │  │ Tenant B Data       │ │                          │
│  │  │ Tenant C Data       │ │                          │
│  │  └─────────────────────┘ │                          │
│  └──────────────────────────┘                          │
│                                                         │
│  ✅ Chi phí thấp (1 server, 1 database)                 │
│  ✅ Dễ maintain & update                                │
│  ✅ Scale tốt hơn                                       │
│  ⚠️ Cần cẩn thận về data isolation                      │
│  ⚠️ Khó customize per tenant                            │
└─────────────────────────────────────────────────────────┘
```

---

## 🏗️ Multi-Tenant Database Strategies

### Strategy 1: Database per Tenant (Isolation tốt nhất)

```
┌─────────────────────────────────────────────────────────┐
│                  Application Server                     │
│                                                         │
│  ┌────────────────────────────────────────────────┐    │
│  │ Tenant Resolver (từ subdomain/header)          │    │
│  └────────────┬───────────────────────────────────┘    │
│               │                                         │
│  ┌────────────▼───────────────────────────────────┐    │
│  │ Connection String Provider                     │    │
│  │ • tenant-a → Server=.;Database=WidgetData_A   │    │
│  │ • tenant-b → Server=.;Database=WidgetData_B   │    │
│  │ • tenant-c → Server=.;Database=WidgetData_C   │    │
│  └────────────┬───────────────────────────────────┘    │
└───────────────┼─────────────────────────────────────────┘
                │
    ┌───────────┴───────────┬───────────────┐
    │                       │               │
┌───▼────────┐      ┌───────▼────┐  ┌──────▼──────┐
│WidgetData_A│      │WidgetData_B│  │WidgetData_C │
│ (Tenant A) │      │ (Tenant B) │  │ (Tenant C)  │
└────────────┘      └────────────┘  └─────────────┘
```

**Ưu điểm:**
- ✅ **Isolation tuyệt đối** - Tenant A không thể access data Tenant B
- ✅ **Backup/Restore dễ** - Mỗi tenant 1 backup file
- ✅ **Performance** - Mỗi DB có indexes riêng
- ✅ **Compliance** - Dễ đáp ứng yêu cầu lưu data ở region riêng

**Nhược điểm:**
- ❌ **Chi phí cao** - N databases cần N storage
- ❌ **Schema migration** - Phải update N databases
- ❌ **Quản lý phức tạp** - N connection strings

**Khi nào dùng:**
- Enterprise customers (trả tiền nhiều)
- Yêu cầu compliance cao (banking, healthcare)
- Cần data residency (EU data phải ở EU)

---

### Strategy 2: Schema per Tenant (Cân bằng)

```
┌─────────────────────────────────────────────────────────┐
│              Single Database: WidgetData                │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌────────────────────────────────────────────────┐    │
│  │ Schema: tenant_a                               │    │
│  │ • Widgets                                      │    │
│  │ • DataSources                                  │    │
│  │ • WidgetExecutions                             │    │
│  └────────────────────────────────────────────────┘    │
│                                                         │
│  ┌────────────────────────────────────────────────┐    │
│  │ Schema: tenant_b                               │    │
│  │ • Widgets                                      │    │
│  │ • DataSources                                  │    │
│  │ • WidgetExecutions                             │    │
│  └────────────────────────────────────────────────┘    │
│                                                         │
│  ┌────────────────────────────────────────────────┐    │
│  │ Schema: shared (Tenants table, etc.)           │    │
│  └────────────────────────────────────────────────┘    │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**Implementation:**
```csharp
public class TenantDbContext : DbContext
{
    private readonly string _tenantSchema;
    
    public TenantDbContext(string tenantSchema)
    {
        _tenantSchema = tenantSchema;
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Set schema cho tất cả tables
        modelBuilder.HasDefaultSchema(_tenantSchema);
        
        modelBuilder.Entity<Widget>().ToTable("Widgets", _tenantSchema);
        modelBuilder.Entity<DataSource>().ToTable("DataSources", _tenantSchema);
    }
}
```

**Ưu điểm:**
- ✅ **Isolation tốt** - Data tách biệt ở schema level
- ✅ **Backup dễ hơn** - Có thể backup per schema
- ✅ **Chi phí thấp hơn** - 1 database server

**Nhược điểm:**
- ⚠️ **SQL Server only** - PostgreSQL, MySQL support limited
- ⚠️ **Migration phức tạp** - Phải tạo N schemas

---

### Strategy 3: Shared Database + TenantId Column (Phổ biến nhất)

```
┌─────────────────────────────────────────────────────────┐
│              Single Database: WidgetData                │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Widgets Table:                                         │
│  ┌────┬─────────────┬──────┬────────────┬────────┐     │
│  │ Id │ TenantId    │ Name │ WidgetType │ ...    │     │
│  ├────┼─────────────┼──────┼────────────┼────────┤     │
│  │ 1  │ tenant-a    │ Sales│ Chart      │ ...    │     │
│  │ 2  │ tenant-a    │ KPIs │ Table      │ ...    │     │
│  │ 3  │ tenant-b    │ Sales│ Chart      │ ...    │     │
│  │ 4  │ tenant-c    │ Inv. │ Metric     │ ...    │     │
│  └────┴─────────────┴──────┴────────────┴────────┘     │
│                                                         │
│  DataSources Table:                                     │
│  ┌────┬─────────────┬──────────┬────────┬────────┐     │
│  │ Id │ TenantId    │ Name     │ Type   │ ...    │     │
│  ├────┼─────────────┼──────────┼────────┼────────┤     │
│  │ 1  │ tenant-a    │ Sales DB │ SQL    │ ...    │     │
│  │ 2  │ tenant-b    │ API      │ REST   │ ...    │     │
│  └────┴─────────────┴──────────┴────────┴────────┘     │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**Ưu điểm:**
- ✅ **Đơn giản nhất** - Chỉ thêm 1 column
- ✅ **Chi phí thấp** - 1 database
- ✅ **Schema migration dễ** - Chỉ 1 lần
- ✅ **Cross-tenant queries** - Có thể query analytics across tenants

**Nhược điểm:**
- ⚠️ **RỦI RO DATA LEAK** - Nếu quên filter TenantId
- ❌ **Performance** - Large table với nhiều tenants
- ❌ **Backup phức tạp** - Không thể backup 1 tenant

**Khi nào dùng:**
- SaaS cho SMBs (nhiều tenants nhỏ)
- Budget hạn chế
- Không có yêu cầu compliance cao

---

## 🔐 Implementation: Shared Database Strategy

### 1. Database Schema

```sql
-- Tenants table (master data)
CREATE TABLE Tenants (
    Id INT PRIMARY KEY IDENTITY,
    TenantKey NVARCHAR(50) UNIQUE NOT NULL, -- 'tenant-a', 'tenant-b'
    Name NVARCHAR(200) NOT NULL,
    Subdomain NVARCHAR(50) UNIQUE, -- 'companya.widgetdata.com'
    ConnectionString NVARCHAR(500), -- Nếu dùng Database per Tenant
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    SubscriptionPlan NVARCHAR(50), -- 'Free', 'Pro', 'Enterprise'
    MaxWidgets INT DEFAULT 10,
    MaxUsers INT DEFAULT 5
);

-- Widgets table với TenantId
CREATE TABLE Widgets (
    Id INT PRIMARY KEY IDENTITY,
    TenantId INT NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    WidgetType NVARCHAR(50) NOT NULL,
    Configuration NVARCHAR(MAX),
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    FOREIGN KEY (TenantId) REFERENCES Tenants(Id),
    INDEX IX_Widgets_TenantId (TenantId) -- ⚠️ QUAN TRỌNG!
);

-- DataSources table với TenantId
CREATE TABLE DataSources (
    Id INT PRIMARY KEY IDENTITY,
    TenantId INT NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    SourceType NVARCHAR(50) NOT NULL,
    ConnectionString NVARCHAR(500),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    FOREIGN KEY (TenantId) REFERENCES Tenants(Id),
    INDEX IX_DataSources_TenantId (TenantId)
);

-- Users table (cross-tenant hoặc per-tenant)
CREATE TABLE AspNetUsers (
    Id NVARCHAR(450) PRIMARY KEY,
    TenantId INT NOT NULL, -- User thuộc tenant nào
    UserName NVARCHAR(256),
    Email NVARCHAR(256),
    -- ... other Identity fields
    
    FOREIGN KEY (TenantId) REFERENCES Tenants(Id),
    INDEX IX_Users_TenantId (TenantId)
);
```

---

### 2. Entity Models

```csharp
// Base entity với TenantId
public abstract class TenantEntity
{
    public int TenantId { get; set; }
    
    // Navigation property
    public Tenant Tenant { get; set; }
}

// Tenant master entity
public class Tenant
{
    public int Id { get; set; }
    public string TenantKey { get; set; } // 'company-a', 'company-b'
    public string Name { get; set; }
    public string Subdomain { get; set; } // 'companya'
    public string ConnectionString { get; set; } // Nếu DB per tenant
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Subscription info
    public string SubscriptionPlan { get; set; }
    public int MaxWidgets { get; set; }
    public int MaxUsers { get; set; }
}

// Widget entity kế thừa TenantEntity
public class Widget : TenantEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string WidgetType { get; set; }
    public string Configuration { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

// DataSource entity
public class DataSource : TenantEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string SourceType { get; set; }
    public string ConnectionString { get; set; }
    public DateTime CreatedAt { get; set; }
}

// User với TenantId
public class ApplicationUser : IdentityUser
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; }
}
```

---

### 3. Tenant Resolution Service

```csharp
public interface ITenantResolver
{
    string GetCurrentTenantKey();
    Task<Tenant> GetCurrentTenantAsync();
}

public class TenantResolver : ITenantResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _context;
    
    public TenantResolver(IHttpContextAccessor httpContextAccessor, ApplicationDbContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
    }
    
    public string GetCurrentTenantKey()
    {
        // Option 1: Từ subdomain
        var host = _httpContextAccessor.HttpContext?.Request.Host.Host;
        if (!string.IsNullOrEmpty(host))
        {
            // companya.widgetdata.com → 'company-a'
            var subdomain = host.Split('.').FirstOrDefault();
            if (subdomain != "www" && subdomain != "widgetdata")
            {
                return subdomain;
            }
        }
        
        // Option 2: Từ JWT claim
        var tenantClaim = _httpContextAccessor.HttpContext?.User.FindFirst("TenantId");
        if (tenantClaim != null)
        {
            return tenantClaim.Value;
        }
        
        // Option 3: Từ header
        var tenantHeader = _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Key"].FirstOrDefault();
        if (!string.IsNullOrEmpty(tenantHeader))
        {
            return tenantHeader;
        }
        
        throw new UnauthorizedAccessException("Tenant not identified");
    }
    
    public async Task<Tenant> GetCurrentTenantAsync()
    {
        var tenantKey = GetCurrentTenantKey();
        
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.TenantKey == tenantKey && t.IsActive);
        
        if (tenant == null)
        {
            throw new UnauthorizedAccessException($"Tenant '{tenantKey}' not found or inactive");
        }
        
        return tenant;
    }
}
```

---

### 4. DbContext với Tenant Filtering

```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ITenantResolver _tenantResolver;
    
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantResolver tenantResolver) : base(options)
    {
        _tenantResolver = tenantResolver;
    }
    
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Widget> Widgets { get; set; }
    public DbSet<DataSource> DataSources { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Global query filter - TỰ ĐỘNG filter theo TenantId
        modelBuilder.Entity<Widget>()
            .HasQueryFilter(w => w.TenantId == GetCurrentTenantId());
        
        modelBuilder.Entity<DataSource>()
            .HasQueryFilter(d => d.TenantId == GetCurrentTenantId());
        
        // Index cho performance
        modelBuilder.Entity<Widget>()
            .HasIndex(w => w.TenantId);
        
        modelBuilder.Entity<DataSource>()
            .HasIndex(d => d.TenantId);
    }
    
    private int GetCurrentTenantId()
    {
        // Sẽ được resolve runtime
        var tenant = _tenantResolver.GetCurrentTenantAsync().Result;
        return tenant?.Id ?? 0;
    }
    
    public override int SaveChanges()
    {
        ApplyTenantId();
        return base.SaveChanges();
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTenantId();
        return await base.SaveChangesAsync(cancellationToken);
    }
    
    private void ApplyTenantId()
    {
        var tenant = _tenantResolver.GetCurrentTenantAsync().Result;
        if (tenant == null) return;
        
        // Tự động set TenantId khi thêm entity mới
        var entries = ChangeTracker.Entries<TenantEntity>()
            .Where(e => e.State == EntityState.Added);
        
        foreach (var entry in entries)
        {
            entry.Entity.TenantId = tenant.Id;
        }
    }
}
```

---

### 5. Repository Pattern với Tenant Filtering

```csharp
public interface IWidgetRepository
{
    Task<List<Widget>> GetAllAsync();
    Task<Widget> GetByIdAsync(int id);
    Task<Widget> AddAsync(Widget widget);
}

public class WidgetRepository : IWidgetRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantResolver _tenantResolver;
    
    public WidgetRepository(ApplicationDbContext context, ITenantResolver tenantResolver)
    {
        _context = context;
        _tenantResolver = tenantResolver;
    }
    
    public async Task<List<Widget>> GetAllAsync()
    {
        // Global query filter tự động apply
        // Chỉ trả về widgets của tenant hiện tại
        return await _context.Widgets.ToListAsync();
    }
    
    public async Task<Widget> GetByIdAsync(int id)
    {
        // Tự động filter theo TenantId
        var widget = await _context.Widgets.FindAsync(id);
        
        if (widget == null)
        {
            throw new NotFoundException($"Widget {id} not found");
        }
        
        return widget;
    }
    
    public async Task<Widget> AddAsync(Widget widget)
    {
        // TenantId tự động set trong SaveChangesAsync
        _context.Widgets.Add(widget);
        await _context.SaveChangesAsync();
        return widget;
    }
}
```

---

### 6. Authentication với Multi-Tenancy

```csharp
public class AuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenGenerator _tokenGenerator;
    
    public async Task<LoginResult> LoginAsync(string email, string password, string tenantKey)
    {
        // Verify tenant exists
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.TenantKey == tenantKey && t.IsActive);
        
        if (tenant == null)
        {
            return new LoginResult { Success = false, Error = "Invalid tenant" };
        }
        
        // Find user trong tenant này
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.TenantId == tenant.Id);
        
        if (user == null)
        {
            return new LoginResult { Success = false, Error = "Invalid credentials" };
        }
        
        // Verify password
        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            return new LoginResult { Success = false, Error = "Invalid credentials" };
        }
        
        // Generate JWT với TenantId claim
        var token = await _tokenGenerator.GenerateTokenAsync(user, new[]
        {
            new Claim("TenantId", tenant.TenantKey),
            new Claim("TenantName", tenant.Name)
        });
        
        return new LoginResult
        {
            Success = true,
            Token = token,
            TenantKey = tenant.TenantKey
        };
    }
}
```

---

### 7. Middleware để Validate Tenant

```csharp
public class TenantValidationMiddleware
{
    private readonly RequestDelegate _next;
    
    public TenantValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context, ITenantResolver tenantResolver)
    {
        // Skip validation cho public endpoints
        if (context.Request.Path.StartsWithSegments("/api/public"))
        {
            await _next(context);
            return;
        }
        
        try
        {
            var tenant = await tenantResolver.GetCurrentTenantAsync();
            
            if (tenant == null || !tenant.IsActive)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Invalid or inactive tenant"
                });
                return;
            }
            
            // Store tenant trong HttpContext cho dùng sau
            context.Items["Tenant"] = tenant;
            
            await _next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
    }
}

// Register middleware
app.UseMiddleware<TenantValidationMiddleware>();
```

---

## 🚀 Tenant Management

### Tenant Registration Flow

```csharp
public class TenantService
{
    public async Task<Tenant> CreateTenantAsync(CreateTenantDto dto)
    {
        // 1. Validate subdomain available
        var existingTenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Subdomain == dto.Subdomain);
        
        if (existingTenant != null)
        {
            throw new ValidationException("Subdomain already taken");
        }
        
        // 2. Create tenant
        var tenant = new Tenant
        {
            TenantKey = GenerateTenantKey(dto.CompanyName),
            Name = dto.CompanyName,
            Subdomain = dto.Subdomain,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            SubscriptionPlan = "Free",
            MaxWidgets = 10,
            MaxUsers = 5
        };
        
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();
        
        // 3. Create admin user cho tenant
        var adminUser = new ApplicationUser
        {
            UserName = dto.AdminEmail,
            Email = dto.AdminEmail,
            TenantId = tenant.Id,
            EmailConfirmed = true
        };
        
        await _userManager.CreateAsync(adminUser, dto.AdminPassword);
        await _userManager.AddToRoleAsync(adminUser, "Admin");
        
        // 4. Seed default data (optional)
        await SeedDefaultDataAsync(tenant.Id);
        
        return tenant;
    }
    
    private string GenerateTenantKey(string companyName)
    {
        // "Acme Corp" → "acme-corp"
        return companyName.ToLower()
            .Replace(" ", "-")
            .Replace("&", "and")
            .Where(c => char.IsLetterOrDigit(c) || c == '-')
            .ToString();
    }
    
    private async Task SeedDefaultDataAsync(int tenantId)
    {
        // Tạo sample data source
        var sampleDataSource = new DataSource
        {
            TenantId = tenantId,
            Name = "Sample Database",
            SourceType = "SQL",
            ConnectionString = "..."
        };
        
        _context.DataSources.Add(sampleDataSource);
        await _context.SaveChangesAsync();
    }
}
```

---

## 🎯 Frontend: Multi-Tenant Login

### Login Flow với Tenant

```razor
@page "/login"

<MudCard>
    <MudCardContent>
        <MudText Typo="Typo.h4">Sign In</MudText>
        
        <!-- Tenant Selection -->
        <MudTextField @bind-Value="subdomain" 
                      Label="Company Subdomain" 
                      Placeholder="your-company"
                      Adornment="Adornment.End"
                      AdornmentText=".widgetdata.com"
                      Required="true" />
        
        <MudTextField @bind-Value="email" 
                      Label="Email" 
                      InputType="InputType.Email"
                      Required="true" />
        
        <MudTextField @bind-Value="password" 
                      Label="Password" 
                      InputType="InputType.Password"
                      Required="true" />
        
        <MudButton Color="Color.Primary" 
                   FullWidth="true" 
                   OnClick="LoginAsync">
            Sign In
        </MudButton>
    </MudCardContent>
</MudCard>

@code {
    private string subdomain;
    private string email;
    private string password;
    
    private async Task LoginAsync()
    {
        var result = await AuthService.LoginAsync(email, password, subdomain);
        
        if (result.Success)
        {
            // Store token
            await LocalStorage.SetItemAsync("token", result.Token);
            await LocalStorage.SetItemAsync("tenant", result.TenantKey);
            
            // Redirect to dashboard
            NavigationManager.NavigateTo("/dashboard");
        }
        else
        {
            Snackbar.Add(result.Error, Severity.Error);
        }
    }
}
```

---

## ⚙️ Configuration

### appsettings.json

```json
{
  "MultiTenancy": {
    "Strategy": "SharedDatabase", // "DatabasePerTenant", "SchemaPerTenant"
    "TenantResolution": "Subdomain", // "Header", "Claim"
    "DefaultTenant": "default",
    "TenantCacheDuration": "00:10:00"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=WidgetData;...",
    "TenantConnectionStringTemplate": "Server=localhost;Database=WidgetData_{TenantKey};..."
  }
}
```

---

## 📊 Tenant Analytics

```csharp
public class TenantAnalyticsService
{
    // Admin analytics - cross tenant
    public async Task<List<TenantStatsDto>> GetAllTenantsStatsAsync()
    {
        // Bypass tenant filter
        var stats = await _context.Tenants
            .Select(t => new TenantStatsDto
            {
                TenantName = t.Name,
                TotalWidgets = t.Widgets.Count(),
                TotalUsers = t.Users.Count(),
                TotalExecutions = t.Widgets.Sum(w => w.Executions.Count()),
                SubscriptionPlan = t.SubscriptionPlan
            })
            .ToListAsync();
        
        return stats;
    }
    
    // Per-tenant analytics
    public async Task<TenantDashboardDto> GetTenantDashboardAsync()
    {
        // Global filter tự động apply
        var widgets = await _context.Widgets.CountAsync();
        var dataSources = await _context.DataSources.CountAsync();
        
        return new TenantDashboardDto
        {
            TotalWidgets = widgets,
            TotalDataSources = dataSources
        };
    }
}
```

---

## 🔒 Security Best Practices

### 1. LUÔN validate TenantId

```csharp
// ❌ NGUY HIỂM - Không validate tenant
[HttpGet("widgets/{id}")]
public async Task<Widget> GetWidget(int id)
{
    return await _context.Widgets.FindAsync(id); // CÓ THỂ LẤY WIDGET CỦA TENANT KHÁC!
}

// ✅ AN TOÀN - Global query filter tự động apply
[HttpGet("widgets/{id}")]
public async Task<Widget> GetWidget(int id)
{
    var widget = await _context.Widgets.FindAsync(id); // Tự động filter TenantId
    
    if (widget == null)
    {
        throw new NotFoundException();
    }
    
    return widget;
}
```

### 2. Unit Testing với Multi-Tenancy

```csharp
public class WidgetServiceTests
{
    [Fact]
    public async Task GetWidgets_OnlyReturnCurrentTenantWidgets()
    {
        // Arrange
        var tenantResolverMock = new Mock<ITenantResolver>();
        tenantResolverMock.Setup(x => x.GetCurrentTenantAsync())
            .ReturnsAsync(new Tenant { Id = 1, TenantKey = "tenant-a" });
        
        var context = CreateInMemoryContext(tenantResolverMock.Object);
        
        // Seed data
        context.Widgets.AddRange(
            new Widget { Id = 1, TenantId = 1, Name = "Widget A1" },
            new Widget { Id = 2, TenantId = 1, Name = "Widget A2" },
            new Widget { Id = 3, TenantId = 2, Name = "Widget B1" } // Tenant khác
        );
        await context.SaveChangesAsync();
        
        var service = new WidgetService(context, tenantResolverMock.Object);
        
        // Act
        var widgets = await service.GetAllAsync();
        
        // Assert
        widgets.Should().HaveCount(2); // Chỉ 2 widgets của tenant-a
        widgets.Should().NotContain(w => w.Name == "Widget B1");
    }
}
```

---

## 📈 Migration Path

### Nếu đã có Single-Tenant, migrate sang Multi-Tenant:

```sql
-- Step 1: Add TenantId column
ALTER TABLE Widgets ADD TenantId INT NULL;
ALTER TABLE DataSources ADD TenantId INT NULL;
ALTER TABLE AspNetUsers ADD TenantId INT NULL;

-- Step 2: Create Tenants table
CREATE TABLE Tenants (...);

-- Step 3: Insert default tenant
INSERT INTO Tenants (TenantKey, Name, IsActive)
VALUES ('default', 'Default Tenant', 1);

-- Step 4: Update existing data
UPDATE Widgets SET TenantId = 1; -- Assign to default tenant
UPDATE DataSources SET TenantId = 1;
UPDATE AspNetUsers SET TenantId = 1;

-- Step 5: Make TenantId NOT NULL
ALTER TABLE Widgets ALTER COLUMN TenantId INT NOT NULL;
ALTER TABLE DataSources ALTER COLUMN TenantId INT NOT NULL;
ALTER TABLE AspNetUsers ALTER COLUMN TenantId INT NOT NULL;

-- Step 6: Add foreign keys & indexes
ALTER TABLE Widgets ADD CONSTRAINT FK_Widgets_Tenants 
    FOREIGN KEY (TenantId) REFERENCES Tenants(Id);
CREATE INDEX IX_Widgets_TenantId ON Widgets(TenantId);
```

---

## 💰 Cost Analysis

### Single-Tenant (10 customers)

| Item | Cost/Month |
|------|------------|
| 10 VMs (B2s) | $400 |
| 10 SQL Databases (S1) | $300 |
| Load Balancer | $20 |
| **Total** | **$720** |

### Multi-Tenant (10 customers)

| Item | Cost/Month |
|------|------------|
| 1 VM (D4s_v3) | $140 |
| 1 SQL Database (S3) | $60 |
| Load Balancer | $20 |
| **Total** | **$220** |

**Tiết kiệm: $500/month (70%)**

---

## ✅ Decision Matrix

| Yêu cầu | Single-Tenant | DB per Tenant | Schema per Tenant | Shared DB |
|---------|---------------|---------------|-------------------|-----------|
| **Budget thấp** | ❌ | ⚠️ | ✅ | ✅ |
| **Nhiều tenants (100+)** | ❌ | ❌ | ⚠️ | ✅ |
| **Data isolation cao** | ✅ | ✅ | ✅ | ⚠️ |
| **Compliance/Regulatory** | ✅ | ✅ | ✅ | ❌ |
| **Customize per tenant** | ✅ | ✅ | ⚠️ | ❌ |
| **Easy maintenance** | ❌ | ❌ | ⚠️ | ✅ |
| **Performance** | ✅ | ✅ | ✅ | ⚠️ |

---

## ⚡ Quick Start: Shared Database + TenantId

### Bước 1: Tạo Tenants Table (5 phút)

```sql
CREATE TABLE Tenants (
    Id INT PRIMARY KEY IDENTITY,
    TenantKey NVARCHAR(50) UNIQUE NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- Insert demo tenants
INSERT INTO Tenants (TenantKey, Name) VALUES 
    ('company-a', 'Company A'),
    ('company-b', 'Company B');
```

### Bước 2: Thêm TenantId vào Tables (10 phút)

```sql
-- Thêm TenantId cho tất cả tables
ALTER TABLE Widgets ADD TenantId INT NOT NULL DEFAULT 1;
ALTER TABLE DataSources ADD TenantId INT NOT NULL DEFAULT 1;
ALTER TABLE AspNetUsers ADD TenantId INT NOT NULL DEFAULT 1;

-- Foreign keys
ALTER TABLE Widgets ADD CONSTRAINT FK_Widgets_Tenants 
    FOREIGN KEY (TenantId) REFERENCES Tenants(Id);
ALTER TABLE DataSources ADD CONSTRAINT FK_DataSources_Tenants 
    FOREIGN KEY (TenantId) REFERENCES Tenants(Id);
ALTER TABLE AspNetUsers ADD CONSTRAINT FK_Users_Tenants 
    FOREIGN KEY (TenantId) REFERENCES Tenants(Id);

-- Indexes (QUAN TRỌNG cho performance!)
CREATE INDEX IX_Widgets_TenantId ON Widgets(TenantId);
CREATE INDEX IX_DataSources_TenantId ON DataSources(TenantId);
CREATE INDEX IX_Users_TenantId ON AspNetUsers(TenantId);
```

### Bước 3: Update Entity Models (15 phút)

```csharp
// Models/Tenant.cs
public class Tenant
{
    public int Id { get; set; }
    public string TenantKey { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Models/TenantEntity.cs - Base class
public abstract class TenantEntity
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; }
}

// Models/Widget.cs - Update existing model
public class Widget : TenantEntity // Thêm kế thừa
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string WidgetType { get; set; }
    // ... existing properties
}

// Models/DataSource.cs
public class DataSource : TenantEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    // ... existing properties
}

// Models/ApplicationUser.cs
public class ApplicationUser : IdentityUser
{
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; }
}
```

### Bước 4: TenantResolver Service (20 phút)

```csharp
// Services/ITenantResolver.cs
public interface ITenantResolver
{
    Task<Tenant> GetCurrentTenantAsync();
    int GetCurrentTenantId();
}

// Services/TenantResolver.cs
public class TenantResolver : ITenantResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _context;
    private Tenant _cachedTenant;
    
    public TenantResolver(
        IHttpContextAccessor httpContextAccessor,
        ApplicationDbContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
    }
    
    public async Task<Tenant> GetCurrentTenantAsync()
    {
        if (_cachedTenant != null)
            return _cachedTenant;
        
        // Lấy TenantId từ JWT claim
        var tenantIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst("TenantId")?.Value;
        
        if (string.IsNullOrEmpty(tenantIdClaim))
            throw new UnauthorizedAccessException("TenantId not found in token");
        
        var tenantId = int.Parse(tenantIdClaim);
        
        _cachedTenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId && t.IsActive);
        
        if (_cachedTenant == null)
            throw new UnauthorizedAccessException($"Tenant {tenantId} not found or inactive");
        
        return _cachedTenant;
    }
    
    public int GetCurrentTenantId()
    {
        return GetCurrentTenantAsync().Result.Id;
    }
}
```

### Bước 5: Update DbContext với Global Query Filter (30 phút)

```csharp
// Data/ApplicationDbContext.cs
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IHttpContextAccessor httpContextAccessor) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Widget> Widgets { get; set; }
    public DbSet<DataSource> DataSources { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // QUAN TRỌNG: Global Query Filters
        // Tự động filter tất cả queries theo TenantId
        modelBuilder.Entity<Widget>()
            .HasQueryFilter(w => w.TenantId == GetCurrentTenantId());
        
        modelBuilder.Entity<DataSource>()
            .HasQueryFilter(d => d.TenantId == GetCurrentTenantId());
        
        // Indexes
        modelBuilder.Entity<Widget>()
            .HasIndex(w => w.TenantId);
        
        modelBuilder.Entity<DataSource>()
            .HasIndex(d => d.TenantId);
    }
    
    private int GetCurrentTenantId()
    {
        var tenantIdClaim = _httpContextAccessor?.HttpContext?.User
            .FindFirst("TenantId")?.Value;
        
        return !string.IsNullOrEmpty(tenantIdClaim) 
            ? int.Parse(tenantIdClaim) 
            : 0;
    }
    
    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        // Tự động set TenantId khi thêm entity mới
        var tenantId = GetCurrentTenantId();
        
        var entries = ChangeTracker.Entries<TenantEntity>()
            .Where(e => e.State == EntityState.Added);
        
        foreach (var entry in entries)
        {
            entry.Entity.TenantId = tenantId;
        }
        
        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

### Bước 6: Update Authentication (20 phút)

```csharp
// Services/AuthService.cs
public class AuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    
    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        var user = await _userManager.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email == email);
        
        if (user == null || !await _userManager.CheckPasswordAsync(user, password))
        {
            return new LoginResult { Success = false, Error = "Invalid credentials" };
        }
        
        if (!user.Tenant.IsActive)
        {
            return new LoginResult { Success = false, Error = "Tenant is inactive" };
        }
        
        // Tạo JWT với TenantId claim
        var token = GenerateJwtToken(user);
        
        return new LoginResult
        {
            Success = true,
            Token = token,
            TenantName = user.Tenant.Name
        };
    }
    
    private string GenerateJwtToken(ApplicationUser user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("TenantId", user.TenantId.ToString()), // ⚠️ QUAN TRỌNG!
            new Claim("TenantKey", user.Tenant.TenantKey)
        };
        
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])
        );
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### Bước 7: Register Services (5 phút)

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// QUAN TRỌNG: HttpContextAccessor cho TenantResolver
builder.Services.AddHttpContextAccessor();

// Register TenantResolver
builder.Services.AddScoped<ITenantResolver, TenantResolver>();

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    );
});

var app = builder.Build();
app.Run();
```

### Bước 8: Test (15 phút)

```csharp
// Test script
using var scope = app.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

// Tạo user cho tenant A
var tenantA = await context.Tenants.FirstAsync(t => t.TenantKey == "company-a");
var userA = new ApplicationUser
{
    UserName = "user.a@companya.com",
    Email = "user.a@companya.com",
    TenantId = tenantA.Id
};
await userManager.CreateAsync(userA, "Password123!");

// Tạo widget cho tenant A
var widget = new Widget
{
    Name = "Sales Dashboard",
    WidgetType = "Chart"
    // TenantId tự động set trong SaveChangesAsync!
};
context.Widgets.Add(widget);
await context.SaveChangesAsync();

// Query - Global Filter tự động apply
var widgets = await context.Widgets.ToListAsync(); // Chỉ thấy widgets của tenant hiện tại!
```

### ✅ Xong! Chỉ mất ~2 giờ

**Kiểm tra:**
- [x] User A login → Chỉ thấy data của Company A
- [x] User B login → Chỉ thấy data của Company B
- [x] Tạo widget mới → Tự động gán TenantId
- [x] Query widgets → Tự động filter theo tenant

---

## 🎯 Khuyến nghị

### Cho Widget Data Project:

**Giai đoạn 1 (MVP - 0-10 customers):**
- ✅ **Shared Database với TenantId**
- Lý do: Đơn giản, chi phí thấp, dễ maintain
- Chuẩn bị sẵn code structure để migrate sau

**Giai đoạn 2 (Growth - 10-50 customers):**
- ✅ **Hybrid**: Shared DB + Option Database per Tenant (cho Enterprise)
- Free/Pro: Shared database
- Enterprise: Dedicated database

**Giai đoạn 3 (Scale - 50+ customers):**
- ✅ **Database per Tenant với Connection Pooling**
- Hoặc **Shard database** (chia data ra nhiều DB servers)

---

## 🚀 Roadmap Implementation

### ⚡ Fast Track (Shared Database - 1-2 ngày)

**Ngày 1 - Morning (4 giờ):**
- [x] Add Tenants table và sample data
- [x] Add TenantId column to all tables
- [x] Update entity models (base class TenantEntity)
- [x] Implement TenantResolver service

**Ngày 1 - Afternoon (4 giờ):**
- [x] Add Global Query Filters to DbContext
- [x] Update SaveChangesAsync (auto-set TenantId)
- [x] Update authentication (add TenantId claim)
- [x] Basic testing

**Ngày 2 - Morning (4 giờ):**
- [ ] Unit tests với multiple tenants
- [ ] Fix any bugs từ testing
- [ ] Security audit (data isolation)
- [ ] Performance testing

**Ngày 2 - Afternoon (4 giờ):**
- [ ] Tenant management API endpoints
- [ ] Documentation
- [ ] Deploy to staging
- [ ] Final testing

### 📈 Future Enhancements (Optional)

**Phase 2 (Tuần 2-3): Admin Features**
- [ ] Tenant registration UI
- [ ] Cross-tenant analytics dashboard
- [ ] Usage limits enforcement (MaxWidgets, MaxUsers)
- [ ] Subscription billing integration

**Phase 3 (Tuần 4-6): Enterprise Features**
- [ ] Database per Tenant option (for premium customers)
- [ ] Data residency (EU/US separation)
- [ ] Tenant-specific customization
- [ ] White-label branding

---

## 🔧 Troubleshooting

### Lỗi: "TenantId not found in token"

```csharp
// Kiểm tra JWT có TenantId claim không
var token = "your-jwt-token";
var handler = new JwtSecurityTokenHandler();
var jwtToken = handler.ReadJwtToken(token);
var tenantIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "TenantId");
Console.WriteLine($"TenantId: {tenantIdClaim?.Value}");
```

**Fix:** Đảm bảo AuthService thêm claim khi generate token.

### Lỗi: Thấy data của tenant khác

```csharp
// Test Global Query Filter
var widgets = await _context.Widgets.ToListAsync();
var distinctTenants = widgets.Select(w => w.TenantId).Distinct();

if (distinctTenants.Count() > 1)
{
    throw new Exception("DATA LEAK! Multiple tenants in result!");
}
```

**Fix:** 
1. Kiểm tra Global Query Filter đã apply chưa
2. Kiểm tra `IgnoreQueryFilters()` có được dùng không
3. Verify TenantResolver return đúng tenant

### Lỗi: TenantId = 0 khi SaveChanges

```csharp
// Debug trong SaveChangesAsync
public override async Task<int> SaveChangesAsync(...)
{
    var tenantId = GetCurrentTenantId();
    Console.WriteLine($"Current TenantId: {tenantId}"); // Debug
    
    if (tenantId == 0)
    {
        throw new InvalidOperationException(
            "Cannot save: TenantId is 0. User not authenticated?"
        );
    }
    // ...
}
```

**Fix:** Đảm bảo user đã login và JWT có TenantId claim.

### Performance: Queries chậm

```sql
-- Kiểm tra indexes
SELECT 
    t.name AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType
FROM sys.indexes i
JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name IN ('Widgets', 'DataSources', 'AspNetUsers')
  AND i.name LIKE '%TenantId%';
```

**Fix:** Tạo indexes trên TenantId columns.

---

## 📊 Monitoring

```csharp
// Log tenant access patterns
public class TenantLoggingMiddleware
{
    public async Task InvokeAsync(HttpContext context, ITenantResolver tenantResolver)
    {
        var tenant = await tenantResolver.GetCurrentTenantAsync();
        
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["TenantId"] = tenant.Id,
            ["TenantKey"] = tenant.TenantKey
        }))
        {
            _logger.LogInformation(
                "Request from tenant {TenantKey}: {Method} {Path}",
                tenant.TenantKey,
                context.Request.Method,
                context.Request.Path
            );
            
            await _next(context);
        }
    }
}
```

---

← [Quay lại INDEX](INDEX.md)
