using WidgetData.API.Middleware;

namespace WidgetData.Tests.Services;

public class WidgetActivityMiddlewareTests
{
    [Theory]
    [InlineData("/api/widgets/5/execute", "GET", 5, "execute")]
    [InlineData("/api/widgets/5/data", "GET", 5, "data")]
    [InlineData("/api/widgets/5/export", "GET", 5, "export")]
    [InlineData("/api/widgets/5/history", "GET", 5, "history")]
    [InlineData("/api/widgets/5/deliver/2", "POST", 5, "deliver")]
    [InlineData("/api/widgets/5", "GET", 5, "view")]
    [InlineData("/api/widgets/5", "PUT", 5, "update")]
    [InlineData("/api/widgets/5", "DELETE", 5, "delete")]
    public void ExtractWidgetActivity_KnownRoutes_ReturnsCorrectValues(
        string path, string method, int expectedWidgetId, string expectedEndpoint)
    {
        var (widgetId, endpoint) = WidgetActivityMiddleware.ExtractWidgetActivity(path, method);

        Assert.Equal(expectedWidgetId, widgetId);
        Assert.Equal(expectedEndpoint, endpoint);
    }

    [Theory]
    [InlineData("/api/datasources/1", "GET")]
    [InlineData("/api/widgets", "GET")]
    [InlineData("/api/widget-activity/5", "GET")]
    [InlineData("/health", "GET")]
    public void ExtractWidgetActivity_UnrelatedRoutes_ReturnsNulls(string path, string method)
    {
        var (widgetId, endpoint) = WidgetActivityMiddleware.ExtractWidgetActivity(path, method);

        Assert.Null(widgetId);
        Assert.Null(endpoint);
    }

    [Theory]
    [InlineData("/api/widgets/5/config-archives", "GET")]
    [InlineData("/api/widgets/5/deliveries", "GET")]
    public void ExtractWidgetActivity_UntrackedSuffix_ReturnsNullEndpoint(string path, string method)
    {
        var (widgetId, endpoint) = WidgetActivityMiddleware.ExtractWidgetActivity(path, method);

        Assert.Null(endpoint);
    }

    [Fact]
    public void ExtractWidgetActivity_NonIntegerId_ReturnsNulls()
    {
        var (widgetId, endpoint) = WidgetActivityMiddleware.ExtractWidgetActivity("/api/widgets/abc/data", "GET");

        Assert.Null(widgetId);
        Assert.Null(endpoint);
    }
}
