using System.Net;

namespace WidgetData.IntegrationTests;

public class ApiSmokeTests : IClassFixture<WidgetApiFactory>
{
    private readonly HttpClient _client;

    public ApiSmokeTests(WidgetApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_Endpoint_Should_ReturnSuccess()
    {
        var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AuthMe_WithoutToken_Should_ReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task FormSchema_ForUnknownWidget_Should_NotReturnServerError()
    {
        var response = await _client.GetAsync("/api/form/999999/schema");
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
