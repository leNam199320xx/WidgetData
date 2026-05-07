using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace WidgetData.IntegrationTests;

public class WidgetApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = "integration-test-secret-key-with-minimum-length-32",
                ["JwtSettings:Issuer"] = "WidgetData.IntegrationTests",
                ["JwtSettings:Audience"] = "WidgetData.IntegrationTests"
            });
        });
    }
}
