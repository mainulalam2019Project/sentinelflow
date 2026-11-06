using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SentinelFlow.Infrastructure;

namespace SentinelFlow.Api;

[ApiController]
[Route("api/metrics")]
[Authorize]
public class MetricsController : ControllerBase
{
    private readonly SentinelFlowDbContext _db;
    private readonly StackExchange.Redis.IConnectionMultiplexer? _redis;

    public MetricsController(SentinelFlowDbContext db, IServiceProvider sp)
    {
        _db = db;
        _redis = sp.GetService<StackExchange.Redis.IConnectionMultiplexer>();
    }

    [HttpGet("{serviceId:guid}")]
    public async Task<IActionResult> Get(Guid serviceId, [FromQuery] string? type, [FromQuery] int hours = 24)
    {
        var cacheKey = $"metrics:{serviceId}:{type}:{hours}";
        if (_redis is not null)
        {
            var cached = await _redis.GetDatabase().StringGetAsync(cacheKey);
            if (cached.HasValue) return Ok(System.Text.Json.JsonSerializer.Deserialize<object>(cached!));
        }

        var since = DateTime.UtcNow.AddHours(-hours);
        var q = _db.Metrics.Where(m => m.ServiceId == serviceId && m.Timestamp >= since);
        if (!string.IsNullOrEmpty(type) && Enum.TryParse<Domain.MetricType>(type, true, out var t)) q = q.Where(m => m.Type == t);
        var data = await q.OrderBy(m => m.Timestamp).Select(m => new { m.Type, m.Value, m.Timestamp }).ToListAsync();

        if (_redis is not null)
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(data);
            await _redis.GetDatabase().StringSetAsync(cacheKey, payload, TimeSpan.FromMinutes(2));
        }
        return Ok(data);
    }
}
