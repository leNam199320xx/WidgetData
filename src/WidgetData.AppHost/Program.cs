var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.WidgetData_API>("widgetdata-api")
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.WidgetData_Worker>("widgetdata-worker")
    .WithReference(api)
    .WaitFor(api);

var gateway = builder.AddProject<Projects.WidgetData_Gateway>("widgetdata-gateway")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.WidgetData_Web>("widgetdata-web")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

// ── Demo web projects ──────────────────────────────────────────────────────
builder.AddProject<Projects.shop_admin>("shop-admin")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
