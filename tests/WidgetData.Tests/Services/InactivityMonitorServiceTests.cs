using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WidgetData.Domain.Entities;
using WidgetData.Infrastructure.Data;
using WidgetData.Infrastructure.Services;
using WidgetData.Tests.TestData;

namespace WidgetData.Tests.Services;

public class InactivityMonitorServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static InactivityMonitorService CreateService(ApplicationDbContext context, int checkIntervalMinutes = 60, int defaultThresholdDays = 30)
    {
        var services = new ServiceCollection();
        services.AddSingleton(context);
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase("ignored"));
        var provider = services.BuildServiceProvider();

        // Override scope factory to return the shared context
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var scopeMock = new Mock<IServiceScope>();
        var spMock = new Mock<IServiceProvider>();
        spMock.Setup(s => s.GetService(typeof(ApplicationDbContext))).Returns(context);
        scopeMock.Setup(s => s.ServiceProvider).Returns(spMock.Object);
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InactivityMonitor:CheckIntervalMinutes"] = checkIntervalMinutes.ToString(),
                ["InactivityMonitor:DefaultThresholdDays"] = defaultThresholdDays.ToString()
            })
            .Build();

        var loggerMock = new Mock<ILogger<InactivityMonitorService>>();
        return new InactivityMonitorService(scopeFactoryMock.Object, loggerMock.Object, config);
    }

    [Fact]
    public async Task RunCheck_AutoDisablesWidget_WhenInactivityAutoDisableEnabled()
    {
        var context = CreateContext();
        var widget = TestDataBuilder.CreateWidget(1);
        widget.DataSource = null!;
        widget.IsActive = true;
        widget.InactivityAutoDisableEnabled = true;
        widget.InactivityThresholdDays = 30;
        widget.LastActivityAt = DateTime.UtcNow.AddDays(-40);
        context.Widgets.Add(widget);
        await context.SaveChangesAsync();

        var service = CreateService(context, defaultThresholdDays: 30);

        // Use reflection to invoke the private RunCheckAsync method
        var method = typeof(InactivityMonitorService)
            .GetMethod("RunCheckAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);
        await (Task)method.Invoke(service, [CancellationToken.None])!;

        var updated = await context.Widgets.FindAsync(1);
        Assert.False(updated!.IsActive);
    }

    [Fact]
    public async Task RunCheck_DoesNotDisableWidget_WhenAutoDisableDisabled()
    {
        var context = CreateContext();
        var widget = TestDataBuilder.CreateWidget(2);
        widget.DataSource = null!;
        widget.IsActive = true;
        widget.InactivityAutoDisableEnabled = false;
        widget.LastActivityAt = DateTime.UtcNow.AddDays(-40);
        context.Widgets.Add(widget);
        await context.SaveChangesAsync();

        var service = CreateService(context, defaultThresholdDays: 30);

        var method = typeof(InactivityMonitorService)
            .GetMethod("RunCheckAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method!.Invoke(service, [CancellationToken.None])!;

        var updated = await context.Widgets.FindAsync(2);
        Assert.True(updated!.IsActive);
    }

    [Fact]
    public async Task RunCheck_RecordsInactivityAlertAuditLog()
    {
        var context = CreateContext();
        var widget = TestDataBuilder.CreateWidget(3);
        widget.DataSource = null!;
        widget.IsActive = true;
        widget.InactivityAutoDisableEnabled = false;
        widget.LastActivityAt = DateTime.UtcNow.AddDays(-40);
        context.Widgets.Add(widget);
        await context.SaveChangesAsync();

        var service = CreateService(context, defaultThresholdDays: 30);

        var method = typeof(InactivityMonitorService)
            .GetMethod("RunCheckAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method!.Invoke(service, [CancellationToken.None])!;

        var alert = await context.AuditLogs.FirstOrDefaultAsync(a => a.Action == "InactivityAlert");
        Assert.NotNull(alert);
        Assert.Equal("Widget", alert.EntityType);
        Assert.Equal("3", alert.EntityId);
    }

    [Fact]
    public async Task RunCheck_SkipsWidgets_WithinThreshold()
    {
        var context = CreateContext();
        var widget = TestDataBuilder.CreateWidget(4);
        widget.DataSource = null!;
        widget.IsActive = true;
        widget.InactivityAutoDisableEnabled = true;
        widget.LastActivityAt = DateTime.UtcNow.AddDays(-5); // well within 30-day threshold
        context.Widgets.Add(widget);
        await context.SaveChangesAsync();

        var service = CreateService(context, defaultThresholdDays: 30);

        var method = typeof(InactivityMonitorService)
            .GetMethod("RunCheckAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method!.Invoke(service, [CancellationToken.None])!;

        var updated = await context.Widgets.FindAsync(4);
        Assert.True(updated!.IsActive);
        Assert.False(await context.AuditLogs.AnyAsync(a => a.Action == "InactivityAlert" && a.EntityId == "4"));
    }

    [Fact]
    public async Task RunCheck_UsesPerWidgetThreshold_WhenSet()
    {
        var context = CreateContext();
        // Widget with custom threshold of 7 days and inactive for 35 days
        // (beyond both the default 30-day and its own 7-day threshold)
        var widget = TestDataBuilder.CreateWidget(5);
        widget.DataSource = null!;
        widget.IsActive = true;
        widget.InactivityAutoDisableEnabled = true;
        widget.InactivityThresholdDays = 7;
        widget.LastActivityAt = DateTime.UtcNow.AddDays(-35);
        context.Widgets.Add(widget);
        await context.SaveChangesAsync();

        var service = CreateService(context, defaultThresholdDays: 30);

        var method = typeof(InactivityMonitorService)
            .GetMethod("RunCheckAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method!.Invoke(service, [CancellationToken.None])!;

        var updated = await context.Widgets.FindAsync(5);
        Assert.False(updated!.IsActive);
    }
}
