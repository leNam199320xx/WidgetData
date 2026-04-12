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
    public DbSet<WidgetGroup> WidgetGroups => Set<WidgetGroup>();
    public DbSet<WidgetGroupMember> WidgetGroupMembers => Set<WidgetGroupMember>();
    public DbSet<UserWidgetPermission> UserWidgetPermissions => Set<UserWidgetPermission>();
    public DbSet<UserGroupPermission> UserGroupPermissions => Set<UserGroupPermission>();
    public DbSet<DeliveryTarget> DeliveryTargets => Set<DeliveryTarget>();
    public DbSet<DeliveryExecution> DeliveryExecutions => Set<DeliveryExecution>();

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

        builder.Entity<WidgetGroupMember>()
            .HasKey(m => new { m.WidgetGroupId, m.WidgetId });

        builder.Entity<WidgetGroupMember>()
            .HasOne(m => m.WidgetGroup)
            .WithMany(g => g.Members)
            .HasForeignKey(m => m.WidgetGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WidgetGroupMember>()
            .HasOne(m => m.Widget)
            .WithMany(w => w.GroupMembers)
            .HasForeignKey(m => m.WidgetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserWidgetPermission>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserWidgetPermission>()
            .HasOne(p => p.Widget)
            .WithMany(w => w.Permissions)
            .HasForeignKey(p => p.WidgetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserGroupPermission>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserGroupPermission>()
            .HasOne(p => p.Group)
            .WithMany(g => g.Permissions)
            .HasForeignKey(p => p.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<DeliveryTarget>()
            .HasOne(d => d.Widget)
            .WithMany(w => w.DeliveryTargets)
            .HasForeignKey(d => d.WidgetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<DeliveryExecution>()
            .HasOne(e => e.DeliveryTarget)
            .WithMany(d => d.Executions)
            .HasForeignKey(e => e.DeliveryTargetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
