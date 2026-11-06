using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SentinelFlow.Infrastructure;

namespace SentinelFlow.Api;

/// <summary>Development-only failure simulator.</summary>
[ApiController]
[Route("api/simulator")]
[Authorize(Policy = "AdminOnly")]
public class SimulatorController : ControllerBase
{
    private readonly SimulatorState _state;

    public SimulatorController(SimulatorState state) => _state = state;

    [HttpPost("latency-spike/{serviceId:guid}")]
    public Task<IActionResult> LatencySpike(Guid serviceId) => SetAsync(serviceId, "latency-spike");

    [HttpPost("error-spike/{serviceId:guid}")]
    public Task<IActionResult> ErrorSpike(Guid serviceId) => SetAsync(serviceId, "error-spike");

    [HttpPost("unavailable/{serviceId:guid}")]
    public Task<IActionResult> Unavailable(Guid serviceId) => SetAsync(serviceId, "unavailable");

    [HttpPost("traffic-spike/{serviceId:guid}")]
    public Task<IActionResult> TrafficSpike(Guid serviceId) => SetAsync(serviceId, "traffic-spike");

    private async Task<IActionResult> SetAsync(Guid serviceId, string mode)
    {
        await _state.SetModeAsync(serviceId, mode);
        return Ok(new { serviceId, mode, note = "Development-only simulation" });
    }

    [HttpPost("clear/{serviceId:guid}")]
    public async Task<IActionResult> Clear(Guid serviceId)
    {
        await _state.ClearAsync(serviceId);
        return Ok(new { serviceId, mode = "none" });
    }
}
