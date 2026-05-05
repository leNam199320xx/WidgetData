using System.Security.Claims;
using WidgetData.Domain.Interfaces;
using WidgetData.Infrastructure.Services;

namespace WidgetData.API.Middleware;

/// <summary>
/// Middleware đọc TenantId từ JWT claim và đặt vào ITenantContext (scoped).
/// Phải chạy SAU UseAuthentication().
/// </summary>
public class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
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

        await _next(context);
    }
}
