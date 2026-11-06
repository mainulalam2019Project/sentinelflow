using SentinelFlow.Domain;

namespace SentinelFlow.Application;

public record MetricCollectedEvent(Guid ServiceId, MetricType Type, double Value, DateTime Timestamp);
public record IncidentCreatedEvent(Guid IncidentId, Guid ServiceId, string Title, string Severity);
