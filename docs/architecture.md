# Architecture

SentinelFlow follows Clean Architecture: Domain → Application → Infrastructure → Api/Worker.

## Projects

| Project | Responsibility |
| --- | --- |
| `SentinelFlow.Domain` | Entities, enums, no dependencies |
| `SentinelFlow.Application` | Anomaly detection, severity policy, dependency analysis, SLA, summary generation, event contracts |
| `SentinelFlow.Infrastructure` | EF Core DbContext, RabbitMQ bus, shared simulator state (Redis) |
| `SentinelFlow.Api` | REST controllers, JWT auth, SignalR hub, OTel, rate limiting, correlation IDs, audit logs |
| `SentinelFlow.Worker` | HealthCheckWorker, AnomalyDetectionWorker, IncidentPipelineWorker |
| `frontend` | Next.js engineering dashboard |

## Event flow

```
HealthCheckWorker  --metrics.collected-->  RabbitMQ
AnomalyDetectionWorker (queue: anomaly.metrics.queue)
  -- on anomaly --> incident row + incident.created event
IncidentPipelineWorker (queue: incident.pipeline.queue)
  --> root-cause analysis, summary, remediation request
MetricsBroadcastService (queue: metrics.broadcast.queue)
  --> SignalR clients (live dashboard)
```

## Data

PostgreSQL stores services, dependencies, health check results, metric samples, incidents, incident events, remediation actions, audit logs, API keys, and Identity users/roles. Redis caches metrics query results (2 min TTL) and holds simulator flags so the API and Worker share one failure-simulation state.
