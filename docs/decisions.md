# Key Decisions

| Decision | Choice | Rationale |
| --- | --- | --- |
| Web API | ASP.NET Core | Mature ecosystem, great DI/testing, strong typing |
| Database | PostgreSQL | Relational integrity, JSONB options, robust |
| Cache | Redis | Fast, TTL, shared state across processes |
| Messaging | RabbitMQ | Topic routing, durable queues, simpler than Kafka for this scale |
| Realtime | SignalR | Native .NET, minimal client code |
| Anomaly detection | Statistical (mean, stddev, % change) | No external AI dependency; deterministic; testable |
| Remediation | Approval-gated | Dangerous actions never auto-execute by default |
