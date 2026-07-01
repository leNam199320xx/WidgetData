var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var widgetDb = postgres.AddDatabase("widgetdata");

var api = builder.AddProject<Projects.WidgetData_API>("widgetdata-api")
    .WithReference(widgetDb)
    .WaitFor(widgetDb)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.WidgetData_Worker>("widgetdata-worker")
    .WithReference(widgetDb)
    .WaitFor(widgetDb)
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

await builder.Build().RunAsync();
