using Microsoft.AspNetCore.SignalR;
using SentinelFlow.Application;
using SentinelFlow.Infrastructure;

namespace SentinelFlow.Api;

/// <summary>Consumes metrics events from RabbitMQ and pushes them to SignalR clients.</summary>
public class MetricsBroadcastService : BackgroundService
{
    private readonly IMessageBus _bus;
    private readonly IHubContext<MetricsHub> _hub;
    private readonly ILogger<MetricsBroadcastService> _logger;

    public MetricsBroadcastService(IMessageBus bus, IHubContext<MetricsHub> hub, ILogger<MetricsBroadcastService> logger)
        => (_bus, _hub, _logger) = (bus, hub, logger);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = await _bus.SubscribeAsync<MetricCollectedEvent>("metrics.broadcast.queue", "metrics.collected", async (evt, ct) =>
        {
            _logger.LogInformation("Broadcasting {Type}={Value} for {ServiceId}", evt.Type, evt.Value, evt.ServiceId);
            await _hub.Clients.All.SendAsync("MetricUpdated", new
            {
                serviceId = evt.ServiceId,
                type = evt.Type.ToString(),
                value = evt.Value,
                timestamp = evt.Timestamp
            }, ct);
        }, stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
