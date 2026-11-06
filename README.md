# SentinelFlow

SentinelFlow is an AI-assisted, real-time API reliability and incident-response platform. It monitors service health, streams live metrics, detects anomalies statistically, creates incidents automatically, analyses dependencies, generates incident summaries, and supports safe, approval-gated remediation.

## Architecture

```
                    +---------------------+
                    |   Next.js frontend  |
                    |  dashboard / pages  |
                    +----------+----------+
                               | REST + SignalR
                               v
                    +---------------------+
                    |  ASP.NET Core API   |<---- JWT auth, rate limiting,
                    |  controllers        |      audit logs, correlation IDs
                    +----+---+-------+-+--+
                         |   |       | |
              EF Core    |   |       | |  stack exchange
                         v   |       v v
                  +-----------+  +---------+  +-------------------+
                  | PostgreSQL|  |  Redis  |  | RabbitMQ          |
                  +-----------+  +---------+  +---------+---------+
                                                       ^
                    +---------------------+             | events
                    | Worker (.NET Host)  |-------------+
                    | health checks,      |  metrics.collected
                    | anomaly detection,  |  incident.created
                    | incident pipeline   |
                    +---------------------+
```

Clean Architecture: `SentinelFlow.Domain` (entities) → `SentinelFlow.Application` (anomaly detection, severity policy, dependency analysis, SLA) → `SentinelFlow.Infrastructure` (EF Core DbContext, RabbitMQ bus, SimulatorState/Redis) → `SentinelFlow.Api` / `SentinelFlow.Worker`.

## Main Features

- Service registry with criticality, environment, SLA target, owner, health-check path
- Background health checks (HTTP status, latency, timeout, SSL) every 15s
- Live metrics (latency, error rate, request count, CPU, memory) streamed over SignalR
- Statistical anomaly detection (rolling mean, std-deviation, % change)
- Automatic incident creation with timeline events
- Dependency-aware root-cause analysis
- AI incident summary derived from real metrics
- Approval-gated remediation actions with audit trail
- Redis cache-aside for metrics endpoint
- RabbitMQ event-driven pipeline: `metrics.collected` → anomaly detector → `incident.created` → incident pipeline
- Development-only failure simulator shared via Redis (`POST /api/simulator/latency-spike/{id}`)
- JWT auth, role-based access (Admin/User), rate limiting, security headers, audit logs, API keys
- OpenTelemetry traces/metrics, Serilog structured logging, correlation IDs
- Prometheus + Grafana in compose; Prometheus ingests OTLP from the API

## Running

### With Docker Compose (recommended)

```bash
docker compose up --build
```

- Frontend: http://localhost:3001
- API (Swagger): http://localhost:8080/swagger
- RabbitMQ console: http://localhost:15672 (guest/guest)
- Prometheus: http://localhost:9090
- Grafana: http://localhost:3000 (admin/admin)

### Locally (development)

1. Start infrastructure: `docker compose -f docker-compose.infra.yml up -d`
2. `dotnet ef database update --project src/SentinelFlow.Infrastructure --startup-project src/SentinelFlow.Api`
3. `dotnet run --project src/SentinelFlow.Api` (http://localhost:5245)
4. `dotnet run --project src/SentinelFlow.Worker`
5. `cd frontend && npm install && npm run dev` (http://localhost:3001)

## Demo credentials

- Admin: `admin@sentinelflow.dev` / `Admin@123`
- User: `user@sentinelflow.dev` / `User@123`

## Demo scenario (5 minutes)

1. `docker compose up --build` and log in as admin.
2. Open `/services` — seeded Payment API, Checkout API, Payment Database.
3. Open `/dashboard` — live latency/error-rate stream.
4. Simulate failure: `curl -X POST -H "Authorization: Bearer $TOKEN" http://localhost:8080/api/simulator/latency-spike/{paymentId}`
5. Watch the dashboard latency spike, then `/incidents` fill with a new incident.
6. Open the incident: AI summary, probable root cause, remediation approval, full timeline.
7. Clear: `curl -X POST .../api/simulator/clear/{paymentId}`; after a few cycles the system recovers and you can resolve the incident.

Note: the anomaly detector trains its baseline from the last ~30 samples, so it needs a clean interval (~8 min) before a freshly injected spike is flagged again.

## Testing

```bash
dotnet test
```

- Unit tests: anomaly detection, severity policy, SLA, dependency analysis, summary generation
- API tests (WebApplicationFactory): auth rejection, login, services endpoint, registration validation

## Trade-offs & limitations

- Prometheus exporter for .NET is consumed via OTLP (Prometheus `--web.enable-otlp-receiver`) because the dedicated Prometheus exporter package is still RC and incompatible with OpenTelemetry 1.19.x.
- JWT refresh tokens are not implemented (8h access tokens); can be added with the Identity token provider pattern.
- Remediation actions are simulated (no real process restart) by design for safety.
- Single-node SignalR; use a backplane (Redis) for multi-instance deployments.
- PostgreSQL `HealthChecks` table should be partitioned/pruned periodically for large deployments.

## Docs

- `docs/architecture.md`, `docs/security.md`, `docs/observability.md`, `docs/decisions.md`
- `docs/adr/` — ADRs
