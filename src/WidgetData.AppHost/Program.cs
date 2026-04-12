var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.WidgetData_API>("widgetdata-api")
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.WidgetData_Gateway>("widgetdata-gateway")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.WidgetData_Web>("widgetdata-web")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
