# Observability

- Serilog structured logs with `CorrelationId` and `RequestId` enrichment
- OpenTelemetry traces (AspNetCore + HttpClient instrumentation) exported via OTLP
- OpenTelemetry metrics (AspNetCore + HttpClient) exported via OTLP; Prometheus ingests OTLP directly
- Grafana dashboards can be provisioned from `deploy/grafana`
- Every response carries `X-Correlation-ID`
