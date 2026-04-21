using System.Diagnostics;
using System.Security.Claims;
using WidgetData.Application.Interfaces;

namespace WidgetData.API.Middleware;

public class WidgetActivityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<WidgetActivityMiddleware> _logger;

    public WidgetActivityMiddleware(RequestDelegate next, ILogger<WidgetActivityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IWidgetActivityService activityService)
    {
        // Only intercept /api/widgets/{id}/... routes that involve a specific endpoint suffix
        var path = context.Request.Path.Value ?? string.Empty;
        var (widgetId, endpoint) = ExtractWidgetActivity(path, context.Request.Method);

        if (widgetId == null || endpoint == null)
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();

        var statusCode = context.Response.StatusCode;
        if (statusCode >= 200 && statusCode < 500)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                await activityService.RecordAsync(widgetId.Value, endpoint, userId, sw.ElapsedMilliseconds, statusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to record widget API activity for widget {WidgetId}", widgetId);
            }
        }
    }

    /// <summary>
    /// Extracts widgetId and a human-readable endpoint name from a path like
    /// /api/widgets/5/data, /api/widgets/5/execute, /api/widgets/5/export, etc.
    /// Returns (null, null) when the path does not match a tracked widget route.
    /// </summary>
    public static (int? widgetId, string? endpoint) ExtractWidgetActivity(string path, string method)
    {
        // Normalise to lower-case for matching
        var lower = path.TrimEnd('/').ToLowerInvariant();

        // Must start with /api/widgets/
        const string prefix = "/api/widgets/";
        if (!lower.StartsWith(prefix)) return (null, null);

        var rest = lower[prefix.Length..]; // e.g. "5/data" or "5"

        // Split on first '/'
        var slashIndex = rest.IndexOf('/');
        if (slashIndex < 0)
        {
            // Plain /api/widgets/{id} — only track GET (view) and DELETE
            if (!int.TryParse(rest, out var wId)) return (null, null);
            var singleEndpoint = method.ToUpperInvariant() switch
            {
                "GET" => "view",
                "PUT" => "update",
                "DELETE" => "delete",
                _ => null
            };
            return (wId, singleEndpoint);
        }

        var idPart = rest[..slashIndex];
        if (!int.TryParse(idPart, out var widgetId)) return (null, null);

        var suffix = rest[(slashIndex + 1)..]; // e.g. "data", "execute", "export", ...

        // Only track known meaningful suffixes
        var ep = suffix switch
        {
            "execute" => "execute",
            "data" => "data",
            "export" => "export",
            "history" => "history",
            _ when suffix == "deliver" || suffix.StartsWith("deliver/") => "deliver",
            _ => null
        };

        return (widgetId, ep);
    }
}
