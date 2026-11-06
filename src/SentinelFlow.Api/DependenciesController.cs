using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SentinelFlow.Domain;
using SentinelFlow.Infrastructure;

namespace SentinelFlow.Api;

[ApiController]
[Route("api/dependencies")]
[Authorize]
public class DependenciesController : ControllerBase
{
    private readonly SentinelFlowDbContext _db;
    public DependenciesController(SentinelFlowDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var deps = await _db.Dependencies.Include(d => d.Service).Include(d => d.DependsOnService)
            .Select(d => new { d.Id, Service = d.Service.Name, DependsOn = d.DependsOnService.Name })
            .ToListAsync();
        return Ok(deps);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create(Guid serviceId, Guid dependsOnServiceId)
    {
        if (serviceId == dependsOnServiceId) return BadRequest(new { error = "A service cannot depend on itself" });
        var dep = new Dependency { ServiceId = serviceId, DependsOnServiceId = dependsOnServiceId };
        _db.Dependencies.Add(dep);
        await _db.SaveChangesAsync();
        return Created("", dep);
    }

    [HttpGet("graph")]
    public async Task<IActionResult> Graph()
    {
        var services = await _db.Services.Select(s => new { s.Id, s.Name, Health = s.Health.ToString() }).ToListAsync();
        var edges = await _db.Dependencies.Select(d => new { d.ServiceId, d.DependsOnServiceId }).ToListAsync();
        return Ok(new { nodes = services, edges });
    }
}
