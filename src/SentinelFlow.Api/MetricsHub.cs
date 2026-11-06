using Microsoft.AspNetCore.SignalR;

namespace SentinelFlow.Api;

public class MetricsHub : Hub
{
    public async Task SubscribeService(string serviceId) => await Groups.AddToGroupAsync(Context.ConnectionId, $"service-{serviceId}");
    public async Task UnsubscribeService(string serviceId) => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"service-{serviceId}");
}
