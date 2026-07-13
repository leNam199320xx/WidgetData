using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WidgetData.Domain.Entities;
using WidgetData.Domain.Interfaces;

namespace WidgetData.Domain;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ITenantContext _tenantContext;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext? tenantContext = null)
        : base(options)
    {
        _tenantContext = tenantContext ?? new TenantContext { IsSuperAdmin = true };
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<PageVersion> PageVersions => Set<PageVersion>();
    public DbSet<PageWidget> PageWidgets => Set<PageWidget>();
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
    public DbSet<WidgetConfigArchive> WidgetConfigArchives => Set<WidgetConfigArchive>();
    public DbSet<WidgetApiActivity> WidgetApiActivities => Set<WidgetApiActivity>();
    public DbSet<IdeaPost> IdeaPosts => Set<IdeaPost>();
    public DbSet<IdeaSubscription> IdeaSubscriptions => Set<IdeaSubscription>();
    public DbSet<IdeaResult> IdeaResults => Set<IdeaResult>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<FormSubmission> FormSubmissions => Set<FormSubmission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Tenant>()
            .HasIndex(t => t.Slug).IsUnique();

        builder.Entity<Page>()
            .HasOne(p => p.Tenant)
            .WithMany(t => t.Pages)
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Page>()
            .HasIndex(p => new { p.TenantId, p.Slug }).IsUnique();

        builder.Entity<PageVersion>()
            .HasOne(v => v.Page)
            .WithMany(p => p.Versions)
            .HasForeignKey(v => v.PageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PageVersion>()
            .HasIndex(v => new { v.PageId, v.VersionNumber })
            .IsUnique();

        builder.Entity<PageWidget>()
            .HasOne(pw => pw.Page)
            .WithMany(p => p.PageWidgets)
            .HasForeignKey(pw => pw.PageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PageWidget>()
            .HasOne(pw => pw.Widget)
            .WithMany()
            .HasForeignKey(pw => pw.WidgetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Tenant)
            .WithMany(t => t.Users)
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.Entity<DataSource>()
            .HasOne(d => d.Tenant)
            .WithMany(t => t.DataSources)
            .HasForeignKey(d => d.TenantId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
        builder.Entity<DataSource>()
            .HasIndex(d => d.IsActive);

        builder.Entity<Widget>()
            .HasOne(w => w.Tenant)
            .WithMany(t => t.Widgets)
            .HasForeignKey(w => w.TenantId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
        builder.Entity<Widget>()
            .HasIndex(w => w.IsActive);

        builder.Entity<Widget>()
            .HasOne(w => w.DataSource)
            .WithMany(d => d.Widgets)
            .HasForeignKey(w => w.DataSourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WidgetGroup>()
            .HasOne(g => g.Tenant)
            .WithMany()
            .HasForeignKey(g => g.TenantId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.Entity<WidgetSchedule>()
            .HasOne(s => s.Widget)
            .WithMany(w => w.Schedules)
            .HasForeignKey(s => s.WidgetId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<WidgetSchedule>()
            .HasIndex(s => s.IsEnabled);

        builder.Entity<WidgetExecution>()
            .HasOne(e => e.Widget)
            .WithMany(w => w.Executions)
            .HasForeignKey(e => e.WidgetId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<WidgetExecution>()
            .HasIndex(e => new { e.WidgetId, e.StartedAt });
        builder.Entity<WidgetExecution>()
            .HasIndex(e => e.Status);
        builder.Entity<WidgetExecution>()
            .HasIndex(e => e.StartedAt);

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

        builder.Entity<WidgetConfigArchive>()
            .HasOne(a => a.Widget)
            .WithMany()
            .HasForeignKey(a => a.WidgetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WidgetApiActivity>()
            .HasOne(a => a.Widget)
            .WithMany(w => w.ApiActivities)
            .HasForeignKey(a => a.WidgetId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<WidgetApiActivity>()
            .HasIndex(a => new { a.WidgetId, a.CalledAt });

        builder.Entity<IdeaPost>()
            .HasOne(p => p.Widget)
            .WithMany()
            .HasForeignKey(p => p.WidgetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdeaSubscription>()
            .HasOne(s => s.Widget)
            .WithMany()
            .HasForeignKey(s => s.WidgetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdeaResult>()
            .HasOne(r => r.IdeaPost)
            .WithMany(p => p.Results)
            .HasForeignKey(r => r.IdeaPostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<IdeaResult>()
            .HasOne(r => r.IdeaSubscription)
            .WithMany(s => s.Results)
            .HasForeignKey(r => r.IdeaSubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<FormSubmission>()
            .HasOne(fs => fs.Widget)
            .WithMany()
            .HasForeignKey(fs => fs.WidgetId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<FormSubmission>()
            .HasIndex(fs => new { fs.WidgetId, fs.SubmittedAt });
        builder.Entity<AuditLog>()
            .HasIndex(a => new { a.Timestamp, a.Action });

        builder.Entity<Widget>()
            .HasQueryFilter(w => _tenantContext.IsSuperAdmin
                || _tenantContext.CurrentTenantId == null
                || w.TenantId == null
                || w.TenantId == _tenantContext.CurrentTenantId);

        builder.Entity<DataSource>()
            .HasQueryFilter(d => _tenantContext.IsSuperAdmin
                || _tenantContext.CurrentTenantId == null
                || d.TenantId == null
                || d.TenantId == _tenantContext.CurrentTenantId);

        builder.Entity<WidgetGroup>()
            .HasQueryFilter(g => _tenantContext.IsSuperAdmin
                || _tenantContext.CurrentTenantId == null
                || g.TenantId == null
                || g.TenantId == _tenantContext.CurrentTenantId);

        builder.Entity<FormSubmission>()
            .HasQueryFilter(fs => _tenantContext.IsSuperAdmin
                || _tenantContext.CurrentTenantId == null
                || fs.TenantId == null
                || fs.TenantId == _tenantContext.CurrentTenantId);

        builder.Entity<Page>()
            .HasQueryFilter(p => _tenantContext.IsSuperAdmin
                || _tenantContext.CurrentTenantId == null
                || p.TenantId == _tenantContext.CurrentTenantId);

        builder.Entity<PageVersion>()
            .HasQueryFilter(v => _tenantContext.IsSuperAdmin
                || _tenantContext.CurrentTenantId == null
                || v.TenantId == _tenantContext.CurrentTenantId);
    }
}
