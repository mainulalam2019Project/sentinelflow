using Microsoft.EntityFrameworkCore;
using SentinelFlow.Application;
using SentinelFlow.Domain;
using SentinelFlow.Infrastructure;

namespace SentinelFlow.Worker;

public class HealthCheckWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IMessageBus _bus;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<HealthCheckWorker> _logger;

    private readonly SimulatorState _simState;

    public HealthCheckWorker(IServiceScopeFactory scopes, IMessageBus bus, IHttpClientFactory http, ILogger<HealthCheckWorker> logger, SimulatorState simState)
        => (_scopes, _bus, _http, _logger, _simState) = (scopes, bus, http, logger, simState);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<SentinelFlowDbContext>();
                var client = _http.CreateClient("healthcheck");
                var services = await db.Services.ToListAsync(stoppingToken);
                foreach (var service in services)
                {
                    var mode = await _simState.GetModeAsync(service.Id);
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    var result = new HealthCheckResult { ServiceId = service.Id, SslValid = service.BaseUrl.StartsWith("https", StringComparison.OrdinalIgnoreCase) };
                    try
                    {
                        if (mode != "unavailable")
                        {
                            var url = service.BaseUrl.TrimEnd('/') + service.HealthCheckPath;
                            var response = await client.GetAsync(url, stoppingToken);
                            sw.Stop();
                            result.StatusCode = (int)response.StatusCode;
                            result.IsHealthy = response.IsSuccessStatusCode;
                            result.LatencyMs = sw.Elapsed.TotalMilliseconds;
                        }
                        else
                        {
                            sw.Stop();
                            result.StatusCode = 0;
                            result.IsHealthy = false;
                            result.Error = "Service unavailable (simulated)";
                            result.LatencyMs = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        result.IsHealthy = false;
                        result.Error = ex.Message;
                        result.LatencyMs = sw.Elapsed.TotalMilliseconds;
                    }

                    if (mode == "latency-spike") result.LatencyMs = 2400 + Random.Shared.Next(0, 600);
                    if (mode == "error-spike") { result.IsHealthy = false; result.StatusCode = 500; }

                    service.Health = !result.IsHealthy ? ServiceHealth.Unhealthy : result.LatencyMs > 1000 ? ServiceHealth.Degraded : ServiceHealth.Healthy;
                    db.HealthChecks.Add(result);

                    var latency = result.LatencyMs <= 0 ? 200 : result.LatencyMs;
                    db.Metrics.Add(new MetricSample { ServiceId = service.Id, Type = MetricType.Latency, Value = latency });
                    db.Metrics.Add(new MetricSample { ServiceId = service.Id, Type = MetricType.ErrorRate, Value = result.IsHealthy ? 0.5 : 17.2 });
                    db.Metrics.Add(new MetricSample { ServiceId = service.Id, Type = MetricType.RequestCount, Value = mode == "traffic-spike" ? 500 : 50 + Random.Shared.Next(0, 20) });
                    db.Metrics.Add(new MetricSample { ServiceId = service.Id, Type = MetricType.CpuUsage, Value = Math.Clamp(result.LatencyMs / 50, 5, 98) });
                    db.Metrics.Add(new MetricSample { ServiceId = service.Id, Type = MetricType.MemoryUsage, Value = 40 + Random.Shared.Next(0, 40) });

                    await db.SaveChangesAsync(stoppingToken);
                    await _bus.PublishAsync("metrics.collected", new MetricCollectedEvent(service.Id, MetricType.Latency, latency, DateTime.UtcNow), stoppingToken);
                    await _bus.PublishAsync("metrics.collected", new MetricCollectedEvent(service.Id, MetricType.ErrorRate, result.IsHealthy ? 0.5 : 17.2, DateTime.UtcNow), stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Health check cycle failed");
            }
        }
    }
}

public class AnomalyDetectionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IMessageBus _bus;
    private readonly ILogger<AnomalyDetectionWorker> _logger;

    public AnomalyDetectionWorker(IServiceScopeFactory scopes, IMessageBus bus, ILogger<AnomalyDetectionWorker> logger)
        => (_scopes, _bus, _logger) = (scopes, bus, logger);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = await _bus.SubscribeAsync<MetricCollectedEvent>("anomaly.metrics.queue", "metrics.collected", async (evt, ct) =>
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<SentinelFlowDbContext>();
                var history = await db.Metrics
                    .Where(m => m.ServiceId == evt.ServiceId && m.Type == evt.Type && m.Timestamp < evt.Timestamp)
                    .OrderByDescending(m => m.Timestamp).Take(30).Select(m => m.Value).ToListAsync(ct);

                var anomaly = AnomalyDetector.Evaluate(evt.Type.ToString(), history.ToArray(), evt.Value);
                if (!anomaly.IsAnomaly) return;

                var openIncident = await db.Incidents.FirstOrDefaultAsync(i => i.ServiceId == evt.ServiceId && i.Status != IncidentStatus.Resolved && i.Status != IncidentStatus.Closed, ct);
                if (openIncident is null)
                {
                    var service = await db.Services.FindAsync(new object?[] { evt.ServiceId }, ct);
                    if (service is null) return;
                    var number = $"INC-{DateTime.UtcNow:yyyy}-{Random.Shared.Next(10000, 99999)}";
                    var severity = IncidentSeverityPolicy.FromMetric(evt.Type, history.Count > 0 && history.Average() > 0 ? Math.Abs(evt.Value - history.Average()) / history.Average() : 5, service.Criticality);
                    var incident = new Incident
                    {
                        ServiceId = service.Id,
                        IncidentNumber = number,
                        Title = $"{evt.Type} anomaly detected on {service.Name}",
                        Severity = severity,
                        BaselineValue = anomaly.Baseline,
                        ObservedValue = anomaly.Observed,
                        Summary = IncidentSummaryGenerator.Generate(service.Name, evt.Type.ToString(), anomaly.Baseline, anomaly.Observed, DateTime.UtcNow, null)
                    };
                    incident.Events.Add(new IncidentEvent { Message = $"{evt.Type} anomaly detected (observed {anomaly.Observed:N1} vs baseline {anomaly.Baseline:N1})" });
                    db.Incidents.Add(incident);
                    await db.SaveChangesAsync(ct);
                    await _bus.PublishAsync("incident.created", new IncidentCreatedEvent(incident.Id, service.Id, incident.Title, incident.Severity.ToString()), ct);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Anomaly processing failed");
            }
        }, stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}

public class IncidentPipelineWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IMessageBus _bus;
    private readonly ILogger<IncidentPipelineWorker> _logger;

    public IncidentPipelineWorker(IServiceScopeFactory scopes, IMessageBus bus, ILogger<IncidentPipelineWorker> logger)
        => (_scopes, _bus, _logger) = (scopes, bus, logger);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = await _bus.SubscribeAsync<IncidentCreatedEvent>("incident.pipeline.queue", "incident.created", async (evt, ct) =>
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<SentinelFlowDbContext>();
                var incident = await db.Incidents.FindAsync(new object?[] { evt.IncidentId }, ct);
                if (incident is null) return;

                incident.Events.Add(new IncidentEvent { Message = "Dependency analysis started" });
                var service = await db.Services.FindAsync(new object?[] { evt.ServiceId }, ct);
                var deps = await db.Dependencies.ToListAsync(ct);
                var health = await db.Services.ToDictionaryAsync(s => s.Id, s => s.Health, ct);
                var names = await db.Services.ToDictionaryAsync(s => s.Id, s => s.Name, ct);
                var analysis = DependencyAnalyzer.Analyze(service!, deps, health, names);
                incident.ProbableRootCause = analysis.ProbableRootCause;
                incident.RootCauseConfidence = analysis.Confidence;
                incident.Summary = IncidentSummaryGenerator.Generate(names[evt.ServiceId], "latency/error", incident.BaselineValue ?? 0, incident.ObservedValue ?? 0, incident.CreatedAt, analysis.ProbableRootCause);
                incident.Events.Add(new IncidentEvent { Message = $"Probable root cause: {analysis.ProbableRootCause} ({analysis.Confidence:P0})" });
                incident.Events.Add(new IncidentEvent { Message = "Incident summary generated" });
                if (incident.Severity == IncidentSeverity.Critical)
                {
                    incident.Remediations.Add(new RemediationAction
                    {
                        IncidentId = incident.Id,
                        Action = "restart-service",
                        Mode = RemediationMode.ApprovalRequired,
                        Status = RemediationStatus.Pending
                    });
                    incident.Events.Add(new IncidentEvent { Message = "Remediation requested — awaiting approval" });
                }
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Incident pipeline failed");
            }
        }, stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
