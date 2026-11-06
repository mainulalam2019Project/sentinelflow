using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SentinelFlow.Domain;
using SentinelFlow.Infrastructure;

namespace SentinelFlow.Api;

[ApiController]
[Route("api/services")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly SentinelFlowDbContext _db;
    public ServicesController(SentinelFlowDbContext db) => _db = db;

    public record ServiceDto(Guid Id, string Name, string? Description, string Environment, string BaseUrl, string Owner, string HealthCheckPath, string Criticality, double SlaTarget, string Health, bool? MonitorEnabled);

    private static ServiceDto Map(MonitoredService s) => new(s.Id, s.Name, s.Description, s.Environment.ToString(), s.BaseUrl, s.Owner, s.HealthCheckPath, s.Criticality.ToString(), s.SlaTarget, s.Health.ToString(), null);

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? environment, [FromQuery] string? criticality, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var q = _db.Services.AsQueryable();
        if (!string.IsNullOrEmpty(environment) && Enum.TryParse<Domain.Environment>(environment, true, out var env)) q = q.Where(s => s.Environment == env);
        if (!string.IsNullOrEmpty(criticality) && Enum.TryParse<Criticality>(criticality, true, out var c)) q = q.Where(s => s.Criticality == c);
        if (!string.IsNullOrEmpty(search)) q = q.Where(s => s.Name.Contains(search));
        var total = await q.CountAsync();
        var items = await q.OrderBy(s => s.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items = items.Select(Map) });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var s = await _db.Services.FindAsync(id);
        return s is null ? NotFound(new { error = "Service not found" }) : Ok(Map(s));
    }

    public record UpsertService(string Name, string? Description, Domain.Environment Environment, string BaseUrl, string Owner, string HealthCheckPath, Criticality Criticality, double SlaTarget);

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create(UpsertService dto)
    {
        var s = new MonitoredService
        {
            Name = dto.Name, Description = dto.Description, Environment = dto.Environment, BaseUrl = dto.BaseUrl,
            Owner = dto.Owner, HealthCheckPath = dto.HealthCheckPath, Criticality = dto.Criticality, SlaTarget = dto.SlaTarget
        };
        _db.Services.Add(s);
        _db.AuditLogs.Add(new AuditLog { UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, Action = "ServiceCreated", EntityType = "Service", EntityId = s.Id.ToString(), Details = s.Name });
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = s.Id }, Map(s));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(Guid id, UpsertService dto)
    {
        var s = await _db.Services.FindAsync(id);
        if (s is null) return NotFound(new { error = "Service not found" });
        s.Name = dto.Name; s.Description = dto.Description; s.Environment = dto.Environment; s.BaseUrl = dto.BaseUrl;
        s.Owner = dto.Owner; s.HealthCheckPath = dto.HealthCheckPath; s.Criticality = dto.Criticality; s.SlaTarget = dto.SlaTarget;
        _db.AuditLogs.Add(new AuditLog { Action = "ServiceUpdated", EntityType = "Service", EntityId = id.ToString() });
        await _db.SaveChangesAsync();
        return Ok(Map(s));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var s = await _db.Services.FindAsync(id);
        if (s is null) return NotFound(new { error = "Service not found" });
        _db.Services.Remove(s);
        _db.AuditLogs.Add(new AuditLog { Action = "ServiceDeleted", EntityType = "Service", EntityId = id.ToString() });
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
