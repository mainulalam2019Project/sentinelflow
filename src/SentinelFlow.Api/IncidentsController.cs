using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SentinelFlow.Domain;
using SentinelFlow.Infrastructure;

namespace SentinelFlow.Api;

[ApiController]
[Route("api/incidents")]
[Authorize]
public class IncidentsController : ControllerBase
{
    private readonly SentinelFlowDbContext _db;
    public IncidentsController(SentinelFlowDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? severity, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var q = _db.Incidents.Include(i => i.Service).AsQueryable();
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<IncidentStatus>(status, true, out var s)) q = q.Where(i => i.Status == s);
        if (!string.IsNullOrEmpty(severity) && Enum.TryParse<IncidentSeverity>(severity, true, out var sv)) q = q.Where(i => i.Severity == sv);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(i => i.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new { i.Id, i.IncidentNumber, Service = i.Service.Name, i.Severity, i.Status, i.Title, i.Summary, i.CreatedAt, i.ResolvedAt, i.ProbableRootCause, i.RootCauseConfidence })
            .ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var i = await _db.Incidents.Include(i => i.Service).Include(i => i.Events).Include(i => i.Remediations).FirstOrDefaultAsync(i => i.Id == id);
        if (i is null) return NotFound(new { error = "Incident not found" });
        return Ok(new { i.Id, i.IncidentNumber, Service = i.Service.Name, i.Severity, i.Status, i.Title, i.Summary, i.BaselineValue, i.ObservedValue, i.ProbableRootCause, i.RootCauseConfidence, i.CreatedAt, i.ResolvedAt, Events = i.Events.OrderBy(e => e.OccurredAt), Remediations = i.Remediations });
    }

    [HttpPost("{id:guid}/acknowledge")]
    public async Task<IActionResult> Acknowledge(Guid id)
    {
        var i = await _db.Incidents.FindAsync(id);
        if (i is null) return NotFound(new { error = "Incident not found" });
        i.Status = IncidentStatus.Acknowledged;
        i.Events.Add(new IncidentEvent { Message = "Incident acknowledged" });
        await _db.SaveChangesAsync();
        return Ok(new { i.Status });
    }

    [HttpPost("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id)
    {
        var i = await _db.Incidents.FindAsync(id);
        if (i is null) return NotFound(new { error = "Incident not found" });
        i.Status = IncidentStatus.Resolved;
        i.ResolvedAt = DateTime.UtcNow;
        i.Events.Add(new IncidentEvent { Message = "System recovered" });
        await _db.SaveChangesAsync();
        return Ok(new { i.Status });
    }
}
