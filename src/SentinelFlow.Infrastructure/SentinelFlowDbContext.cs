using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SentinelFlow.Domain;

namespace SentinelFlow.Infrastructure;

public class SentinelFlowDbContext : IdentityDbContext<ApplicationUser>
{
    public SentinelFlowDbContext(DbContextOptions<SentinelFlowDbContext> options) : base(options) { }

    public DbSet<MonitoredService> Services => Set<MonitoredService>();
    public DbSet<Dependency> Dependencies => Set<Dependency>();
    public DbSet<HealthCheckResult> HealthChecks => Set<HealthCheckResult>();
    public DbSet<MetricSample> Metrics => Set<MetricSample>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentEvent> IncidentEvents => Set<IncidentEvent>();
    public DbSet<RemediationAction> RemediationActions => Set<RemediationAction>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<MonitoredService>(e =>
        {
            e.HasIndex(s => s.Name);
        });

        builder.Entity<Dependency>(e =>
        {
            e.HasOne(d => d.Service).WithMany().HasForeignKey(d => d.ServiceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(d => d.DependsOnService).WithMany().HasForeignKey(d => d.DependsOnServiceId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(d => new { d.ServiceId, d.DependsOnServiceId }).IsUnique();
        });

        builder.Entity<HealthCheckResult>(e => e.HasIndex(h => new { h.ServiceId, h.CheckedAt }));
        builder.Entity<MetricSample>(e => e.HasIndex(m => new { m.ServiceId, m.Type, m.Timestamp }));
        builder.Entity<Incident>(e => e.HasIndex(i => new { i.ServiceId, i.Status }));
        builder.Entity<IncidentEvent>(e => e.HasIndex(ev => ev.IncidentId));
        builder.Entity<ApiKey>(e => e.HasIndex(k => k.KeyHash).IsUnique());
    }
}
