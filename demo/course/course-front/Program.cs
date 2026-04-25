var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapDefaultEndpoints();

app.Run();
