using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WidgetData.Domain.Entities;

namespace WidgetData.Infrastructure.Data;

/// <summary>
/// IdentityDbContext: Manages only Identity/User management and multi-tenancy
/// - ApplicationUser (with TenantId for multi-tenancy)
/// - Tenant configurations
/// - RefreshToken for JWT
/// - AuditLog for activity tracking
/// </summary>
public class IdentityDbContext : IdentityDbContext<ApplicationUser>
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── Tenant ────────────────────────────────────────────────────────────
        builder.Entity<Tenant>()
            .HasIndex(t => t.Slug).IsUnique();

        // Keep identity context focused on tenant + users only.
        // Other tenant navigation collections belong to ApplicationDbContext.
        builder.Entity<Tenant>().Ignore(t => t.DataSources);
        builder.Entity<Tenant>().Ignore(t => t.Widgets);
        builder.Entity<Tenant>().Ignore(t => t.Pages);
        builder.Entity<Tenant>().Ignore(t => t.PageVersions);

        // ── ApplicationUser → Tenant ──────────────────────────────────────────
        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Tenant)
            .WithMany(t => t.Users)
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // ── RefreshToken ──────────────────────────────────────────────────────
        builder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── AuditLog ──────────────────────────────────────────────────────────
        builder.Entity<AuditLog>()
            .HasIndex(a => new { a.Timestamp, a.Action });
    }
}
