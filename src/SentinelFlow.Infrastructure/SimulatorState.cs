using StackExchange.Redis;

namespace SentinelFlow.Infrastructure;

/// <summary>
/// Shared development-only failure simulator state backed by Redis,
/// so the API and Worker processes see the same simulation flags.
/// </summary>
public class SimulatorState
{
    private readonly IConnectionMultiplexer _redis;

    public SimulatorState(IConnectionMultiplexer redis) => _redis = redis;

    private static string Key(Guid serviceId) => $"sim:{serviceId}";

    public Task SetModeAsync(Guid serviceId, string mode) =>
        _redis.GetDatabase().StringSetAsync(Key(serviceId), mode);

    public async Task<string?> GetModeAsync(Guid serviceId)
    {
        var value = await _redis.GetDatabase().StringGetAsync(Key(serviceId));
        return value.HasValue ? (string?)value : null;
    }

    public Task ClearAsync(Guid serviceId) =>
        _redis.GetDatabase().KeyDeleteAsync(Key(serviceId));
}
