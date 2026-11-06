using Microsoft.AspNetCore.Identity;

namespace SentinelFlow.Domain;

public enum Environment { Development, Staging, Production }
public enum Criticality { Low, Medium, High, Critical }
public enum ServiceHealth { Unknown, Healthy, Degraded, Unhealthy }
public enum MetricType { RequestCount, Latency, ErrorRate, CpuUsage, MemoryUsage, Throughput, Uptime }
public enum IncidentSeverity { Info, Warning, High, Critical }
public enum IncidentStatus { Open, Acknowledged, Resolved, Closed }
public enum RemediationMode { Auto, ApprovalRequired, Disabled }
public enum RemediationStatus { Pending, Approved, Executed, Failed, Rejected }

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class MonitoredService
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public Environment Environment { get; set; }
    public string BaseUrl { get; set; } = "";
    public string Owner { get; set; } = "";
    public string HealthCheckPath { get; set; } = "/health";
    public Criticality Criticality { get; set; }
    public double SlaTarget { get; set; } = 99.9;
    public ServiceHealth Health { get; set; } = ServiceHealth.Unknown;
    public RemediationMode RemediationMode { get; set; } = RemediationMode.ApprovalRequired;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<HealthCheckResult> HealthChecks { get; set; } = new();
    public List<MetricSample> Metrics { get; set; } = new();
    public List<Incident> Incidents { get; set; } = new();
}

public class Dependency
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public MonitoredService Service { get; set; } = null!;
    public Guid DependsOnServiceId { get; set; }
    public MonitoredService DependsOnService { get; set; } = null!;
}

public class HealthCheckResult
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public MonitoredService Service { get; set; } = null!;
    public int StatusCode { get; set; }
    public double LatencyMs { get; set; }
    public bool IsHealthy { get; set; }
    public bool TimedOut { get; set; }
    public bool SslValid { get; set; }
    public string? Error { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

public class MetricSample
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public MonitoredService Service { get; set; } = null!;
    public MetricType Type { get; set; }
    public double Value { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class Incident
{
    public Guid Id { get; set; }
    public string IncidentNumber { get; set; } = "";
    public Guid ServiceId { get; set; }
    public MonitoredService Service { get; set; } = null!;
    public IncidentSeverity Severity { get; set; }
    public IncidentStatus Status { get; set; } = IncidentStatus.Open;
    public string Title { get; set; } = "";
    public string? Summary { get; set; }
    public double? BaselineValue { get; set; }
    public double? ObservedValue { get; set; }
    public string? ProbableRootCause { get; set; }
    public double? RootCauseConfidence { get; set; }
    public List<IncidentEvent> Events { get; set; } = new();
    public List<RemediationAction> Remediations { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}

public class IncidentEvent
{
    public Guid Id { get; set; }
    public Guid IncidentId { get; set; }
    public Incident Incident { get; set; } = null!;
    public string Message { get; set; } = "";
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

public class RemediationAction
{
    public Guid Id { get; set; }
    public Guid IncidentId { get; set; }
    public Incident Incident { get; set; } = null!;
    public string Action { get; set; } = "";
    public RemediationMode Mode { get; set; }
    public RemediationStatus Status { get; set; } = RemediationStatus.Pending;
    public string? RequestedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public string? Result { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExecutedAt { get; set; }
}

public class AuditLog
{
    public Guid Id { get; set; }
    public string? UserId { get; set; }
    public string Action { get; set; } = "";
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ApiKey
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string KeyHash { get; set; } = "";
    public string? Owner { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
}
