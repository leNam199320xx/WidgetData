using Serilog;
using WidgetData.Infrastructure;
using WidgetData.Worker.Workers;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.AddServiceDefaults();

    builder.Services.AddSerilog((sp, lc) => lc
        .WriteTo.Console()
        .ReadFrom.Configuration(builder.Configuration));

    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddHostedService<SchedulerWorkerService>();

    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "WidgetData.Worker terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
