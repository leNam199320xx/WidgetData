# Security & Bảo mật

## 📋 Tổng quan

Widget Data áp dụng **8 lớp bảo mật** để đảm bảo an toàn dữ liệu và hệ thống:

1. ✅ **Authentication** (Xác thực người dùng)
2. ✅ **Authorization** (Phân quyền truy cập)
3. ✅ **Data Encryption** (Mã hóa dữ liệu)
4. ✅ **API Security** (Bảo mật API)
5. ✅ **Audit Logging** (Ghi log kiểm toán)
6. ✅ **Input Validation** (Kiểm tra đầu vào)
7. ✅ **Rate Limiting** (Giới hạn request)
8. ✅ **Compliance** (Tuân thủ quy định)

---

## 🔐 1. Authentication (Xác thực)

### ASP.NET Core Identity

```csharp
// Startup.cs / Program.cs
services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password requirements
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    
    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();
```

### JWT Authentication

```csharp
// JWT Configuration
services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = Configuration["Jwt:Issuer"],
        ValidAudience = Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(Configuration["Jwt:SecretKey"])
        ),
        ClockSkew = TimeSpan.Zero
    };
});

// Generate JWT Token
public string GenerateJwtToken(ApplicationUser user, IList<string> roles)
{
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Name, user.UserName),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };
    
    foreach (var role in roles)
    {
        claims.Add(new Claim(ClaimTypes.Role, role));
    }
    
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    
    var token = new JwtSecurityToken(
        issuer: _configuration["Jwt:Issuer"],
        audience: _configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddHours(2),
        signingCredentials: creds
    );
    
    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

### Multi-Factor Authentication (MFA)

```csharp
// Enable MFA for user
public async Task<bool> EnableMfaAsync(string userId)
{
    var user = await _userManager.FindByIdAsync(userId);
    var key = await _userManager.GetAuthenticatorKeyAsync(user);
    
    if (string.IsNullOrEmpty(key))
    {
        await _userManager.ResetAuthenticatorKeyAsync(user);
        key = await _userManager.GetAuthenticatorKeyAsync(user);
    }
    
    return true;
}

// Verify MFA Code
public async Task<bool> VerifyMfaCodeAsync(string userId, string code)
{
    var user = await _userManager.FindByIdAsync(userId);
    var isValid = await _userManager.VerifyTwoFactorTokenAsync(
        user, 
        _userManager.Options.Tokens.AuthenticatorTokenProvider, 
        code
    );
    
    return isValid;
}
```

### Social Login (OAuth 2.0)

```csharp
// Google Authentication
services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = Configuration["Authentication:Google:ClientSecret"];
    })
    .AddMicrosoftAccount(options =>
    {
        options.ClientId = Configuration["Authentication:Microsoft:ClientId"];
        options.ClientSecret = Configuration["Authentication:Microsoft:ClientSecret"];
    });
```

---

## 🔒 2. Authorization (Phân quyền)

### Role-Based Access Control (RBAC)

```csharp
// Define Roles
public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Developer = "Developer";
    public const string Viewer = "Viewer";
}

// Seed Roles
public async Task SeedRolesAsync()
{
    var roles = new[] { Roles.Admin, Roles.Manager, Roles.Developer, Roles.Viewer };
    
    foreach (var role in roles)
    {
        if (!await _roleManager.RoleExistsAsync(role))
        {
            await _roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

// Controller Authorization
[Authorize(Roles = "Admin,Manager")]
[HttpPost("widgets")]
public async Task<IActionResult> CreateWidget([FromBody] WidgetDto dto)
{
    // Only Admin & Manager can create widgets
    var widget = await _widgetService.CreateAsync(dto);
    return Ok(widget);
}

[Authorize(Roles = "Admin")]
[HttpDelete("widgets/{id}")]
public async Task<IActionResult> DeleteWidget(int id)
{
    // Only Admin can delete
    await _widgetService.DeleteAsync(id);
    return NoContent();
}
```

### Policy-Based Authorization

```csharp
// Define Policies
services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("RequireEmailVerified", policy =>
        policy.RequireClaim("email_verified", "true"));
    
    options.AddPolicy("WidgetOwner", policy =>
        policy.Requirements.Add(new WidgetOwnerRequirement()));
});

// Custom Requirement
public class WidgetOwnerRequirement : IAuthorizationRequirement { }

public class WidgetOwnerHandler : AuthorizationHandler<WidgetOwnerRequirement, Widget>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WidgetOwnerRequirement requirement,
        Widget widget)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (widget.CreatedBy == userId || context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
        }
        
        return Task.CompletedTask;
    }
}

// Usage in Controller
[Authorize(Policy = "WidgetOwner")]
[HttpPut("widgets/{id}")]
public async Task<IActionResult> UpdateWidget(int id, [FromBody] WidgetDto dto)
{
    var widget = await _widgetService.UpdateAsync(id, dto);
    return Ok(widget);
}
```

### Resource-Based Authorization

```csharp
// In Controller
[HttpPut("widgets/{id}")]
public async Task<IActionResult> UpdateWidget(int id, [FromBody] WidgetDto dto)
{
    var widget = await _widgetService.GetByIdAsync(id);
    
    var authResult = await _authorizationService.AuthorizeAsync(
        User, 
        widget, 
        "WidgetOwner"
    );
    
    if (!authResult.Succeeded)
    {
        return Forbid();
    }
    
    var updated = await _widgetService.UpdateAsync(id, dto);
    return Ok(updated);
}
```

---

## 🔐 3. Data Encryption

### Encryption at Rest

```csharp
// Encrypt sensitive fields in database
public class EncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;
    
    public EncryptionService(IConfiguration configuration)
    {
        _key = Convert.FromBase64String(configuration["Encryption:Key"]);
        _iv = Convert.FromBase64String(configuration["Encryption:IV"]);
    }
    
    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        
        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }
        
        return Convert.ToBase64String(ms.ToArray());
    }
    
    public string Decrypt(string cipherText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        
        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        
        var buffer = Convert.FromBase64String(cipherText);
        using var ms = new MemoryStream(buffer);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        
        return sr.ReadToEnd();
    }
}

// Entity with encrypted fields
public class DataSource
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    [Encrypted] // Custom attribute
    public string ConnectionString { get; set; }
    
    [Encrypted]
    public string ApiKey { get; set; }
}
```

### Encryption in Transit (HTTPS/TLS)

```csharp
// Force HTTPS in production
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (!env.IsDevelopment())
    {
        app.UseHttpsRedirection();
        app.UseHsts();
    }
}

// appsettings.json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://localhost:5001",
        "Certificate": {
          "Path": "certificate.pfx",
          "Password": "your-password"
        }
      }
    }
  }
}
```

### Secrets Management

```bash
# User Secrets (Development)
dotnet user-secrets init
dotnet user-secrets set "Jwt:SecretKey" "your-secret-key"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection"

# Azure Key Vault (Production)
services.AddAzureKeyVault(
    new Uri(Configuration["KeyVault:Vault"]),
    new DefaultAzureCredential()
);
```

---

## 🛡️ 4. API Security

### CORS Configuration

```csharp
services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", builder =>
    {
        builder.WithOrigins("https://yourdomain.com")
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();
    });
});

app.UseCors("AllowSpecificOrigins");
```

### Rate Limiting

```csharp
// Install: AspNetCoreRateLimit
services.AddMemoryCache();
services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
services.AddInMemoryRateLimiting();
services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// appsettings.json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 60
      },
      {
        "Endpoint": "*/api/widgets",
        "Period": "1h",
        "Limit": 100
      }
    ]
  }
}
```

### Anti-CSRF Tokens

```csharp
// For Blazor Server
services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

// In Razor Pages
@inject Microsoft.AspNetCore.Antiforgery.IAntiforgery Antiforgery
@{
    var token = Antiforgery.GetAndStoreTokens(Context).RequestToken;
}
<input type="hidden" name="__RequestVerificationToken" value="@token" />
```

---

## 📝 5. Audit Logging

```csharp
public class AuditLog
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string Action { get; set; } // CREATE, UPDATE, DELETE, VIEW
    public string EntityType { get; set; } // Widget, DataSource, etc.
    public int EntityId { get; set; }
    public string Changes { get; set; } // JSON of changes
    public string IpAddress { get; set; }
    public DateTime Timestamp { get; set; }
}

public class AuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContext;
    
    public async Task LogAsync(string action, string entityType, int entityId, object changes = null)
    {
        var userId = _httpContext.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var ipAddress = _httpContext.HttpContext.Connection.RemoteIpAddress?.ToString();
        
        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Changes = changes != null ? JsonSerializer.Serialize(changes) : null,
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        };
        
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}

// Usage
await _auditService.LogAsync("CREATE", "Widget", widget.Id, new { widget.Name, widget.Type });
```

---

## ✅ 6. Input Validation

```csharp
public class WidgetDto
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; }
    
    [Required]
    [RegularExpression(@"^(table|chart|metric|map)$")]
    public string WidgetType { get; set; }
    
    [Range(1, int.MaxValue)]
    public int DataSourceId { get; set; }
    
    [Url]
    public string ApiEndpoint { get; set; }
}

// SQL Injection Prevention
public async Task<List<Widget>> GetWidgetsByNameAsync(string name)
{
    // ✅ GOOD: Parameterized query
    return await _context.Widgets
        .Where(w => w.Name.Contains(name))
        .ToListAsync();
    
    // ❌ BAD: String concatenation
    // var sql = $"SELECT * FROM Widgets WHERE Name LIKE '%{name}%'";
}
```

---

## 📊 7. Security Headers

```csharp
app.Use(async (context, next) =>
{
    // Content Security Policy
    context.Response.Headers.Add("Content-Security-Policy", 
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'");
    
    // X-Frame-Options
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    
    // X-Content-Type-Options
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    
    // X-XSS-Protection
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    
    // Referrer-Policy
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    
    await next();
});
```

---

## 🌍 8. Compliance & Standards

### GDPR Compliance

- **Right to Access**: API để user export data của họ
- **Right to Deletion**: Soft delete với flag `IsDeleted`
- **Data Minimization**: Chỉ thu thập data cần thiết
- **Consent**: Checkbox đồng ý trước khi thu thập data

```csharp
// Export user data
[HttpGet("me/export")]
public async Task<IActionResult> ExportMyData()
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var data = await _userService.ExportUserDataAsync(userId);
    
    return File(
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data)),
        "application/json",
        "my-data.json"
    );
}

// Delete user data
[HttpDelete("me")]
public async Task<IActionResult> DeleteMyAccount()
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    await _userService.DeleteUserAsync(userId); // Soft delete
    
    return NoContent();
}
```

### OWASP Top 10 Mitigation

| Threat | Mitigation |
|--------|------------|
| **A01:2021 – Broken Access Control** | RBAC, Policy-based authorization |
| **A02:2021 – Cryptographic Failures** | AES-256 encryption, HTTPS/TLS |
| **A03:2021 – Injection** | Parameterized queries, Input validation |
| **A04:2021 – Insecure Design** | Threat modeling, Security by design |
| **A05:2021 – Security Misconfiguration** | Secure defaults, Security headers |
| **A06:2021 – Vulnerable Components** | Dependabot, Regular updates |
| **A07:2021 – Authentication Failures** | MFA, Strong password policy |
| **A08:2021 – Integrity Failures** | Code signing, SRI hashes |
| **A09:2021 – Logging Failures** | Comprehensive audit logging |
| **A10:2021 – SSRF** | Whitelist URLs, Input validation |

---

## 🔍 Security Checklist

### Development
- [ ] All passwords hashed with bcrypt/PBKDF2
- [ ] Secrets stored in User Secrets / Key Vault
- [ ] Input validation on all endpoints
- [ ] SQL injection prevention (EF Core)
- [ ] XSS prevention (encode output)
- [ ] CSRF tokens on forms

### Production
- [ ] HTTPS enforced (HSTS)
- [ ] Security headers configured
- [ ] Rate limiting enabled
- [ ] CORS configured properly
- [ ] Audit logging active
- [ ] MFA enabled for admins
- [ ] Regular security updates
- [ ] Penetration testing completed

---

## 📚 Resources

- [ASP.NET Core Security](https://docs.microsoft.com/aspnet/core/security/)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [Azure Key Vault](https://azure.microsoft.com/services/key-vault/)
- [GDPR Compliance](https://gdpr.eu/)

---

← [Quay lại INDEX](INDEX.md)
