using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Data;
using WidgetData.Infrastructure.Services;

namespace WidgetData.API.Middleware;

/// <summary>
/// Middleware đọc TenantId từ:
///   1. JWT claim tenant_id (khi đã đăng nhập)
///   2. Header X-Tenant-Id  (integer, dùng cho public embed)
///   3. Header X-Tenant-Slug (slug string, cần DB lookup, dùng cho public embed)
/// Phải chạy SAU UseAuthentication().
/// </summary>
public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext, ApplicationDbContext dbContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var isSuperAdmin = context.User.IsInRole("SuperAdmin");
            tenantContext.IsSuperAdmin = isSuperAdmin;

            if (!isSuperAdmin)
            {
                var tenantClaim = context.User.FindFirstValue("tenant_id");
                if (int.TryParse(tenantClaim, out var tenantId))
                    tenantContext.CurrentTenantId = tenantId;
            }
        }

        // Fall back to HTTP headers for anonymous / embed requests
        if (!tenantContext.IsSuperAdmin && !tenantContext.CurrentTenantId.HasValue)
        {
            var headerTenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (int.TryParse(headerTenantId, out var headerId))
            {
                tenantContext.CurrentTenantId = headerId;
            }
            else
            {
                var headerSlug = context.Request.Headers["X-Tenant-Slug"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(headerSlug))
                {
                    var tenant = await dbContext.Tenants
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Slug == headerSlug && t.IsActive);
                    if (tenant != null)
                        tenantContext.CurrentTenantId = tenant.Id;
                }
            }
        }

        await _next(context);
    }
}
