using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WidgetData.Domain.Entities;

namespace WidgetData.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<DataSource> DataSources => Set<DataSource>();
    public DbSet<Widget> Widgets => Set<Widget>();
    public DbSet<WidgetSchedule> WidgetSchedules => Set<WidgetSchedule>();
    public DbSet<WidgetExecution> WidgetExecutions => Set<WidgetExecution>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Widget>()
            .HasOne(w => w.DataSource)
            .WithMany(d => d.Widgets)
            .HasForeignKey(w => w.DataSourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WidgetSchedule>()
            .HasOne(s => s.Widget)
            .WithMany(w => w.Schedules)
            .HasForeignKey(s => s.WidgetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WidgetExecution>()
            .HasOne(e => e.Widget)
            .WithMany(w => w.Executions)
            .HasForeignKey(e => e.WidgetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
