using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SentinelFlow.Domain;
using SentinelFlow.Infrastructure;

namespace SentinelFlow.Api;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = "AdminOnly")]
public class AuditLogsController : ControllerBase
{
    private readonly SentinelFlowDbContext _db;
    public AuditLogsController(SentinelFlowDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var logs = await _db.AuditLogs.OrderByDescending(l => l.Timestamp).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(logs);
    }
}

[ApiController]
[Route("api/apikeys")]
[Authorize(Policy = "AdminOnly")]
public class ApiKeysController : ControllerBase
{
    private readonly SentinelFlowDbContext _db;
    public ApiKeysController(SentinelFlowDbContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Create(string name, string? owner)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        _db.ApiKeys.Add(new ApiKey { Name = name, Owner = owner, KeyHash = hash });
        await _db.SaveChangesAsync();
        return Ok(new { apiKey = raw, note = "Shown once only" });
    }

    [HttpGet]
    public async Task<IActionResult> List() => Ok(await _db.ApiKeys.Select(k => new { k.Id, k.Name, k.Owner, k.IsRevoked, k.CreatedAt, k.LastUsedAt }).ToListAsync());

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id)
    {
        var k = await _db.ApiKeys.FindAsync(id);
        if (k is null) return NotFound();
        k.IsRevoked = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
