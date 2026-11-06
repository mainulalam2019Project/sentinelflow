using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SentinelFlow.Domain;
using SentinelFlow.Infrastructure;

namespace SentinelFlow.Api;

[ApiController]
[Route("api/remediations")]
[Authorize]
public class RemediationsController : ControllerBase
{
    private readonly SentinelFlowDbContext _db;
    public RemediationsController(SentinelFlowDbContext db) => _db = db;

    [HttpPost("{incidentId:guid}/approve")]
    public async Task<IActionResult> Approve(Guid incidentId)
    {
        var incident = await _db.Incidents.Include(i => i.Remediations).FirstOrDefaultAsync(i => i.Id == incidentId);
        if (incident is null) return NotFound(new { error = "Incident not found" });

        var remediation = incident.Remediations.FirstOrDefault(r => r.Status == RemediationStatus.Pending);
        if (remediation is null)
        {
            remediation = new RemediationAction
            {
                IncidentId = incident.Id,
                Action = "restart-service",
                Mode = incident.Service.RemediationMode,
                Status = RemediationStatus.Approved,
                RequestedBy = User.Identity?.Name,
                ApprovedBy = User.Identity?.Name,
                ExecutedAt = DateTime.UtcNow,
                Result = "Simulated restart executed"
            };
            _db.RemediationActions.Add(remediation);
            incident.Events.Add(new IncidentEvent { Message = "Engineer approved remediation" });
        }
        else
        {
            remediation.Status = RemediationStatus.Approved;
            remediation.ApprovedBy = User.Identity?.Name;
            remediation.ExecutedAt = DateTime.UtcNow;
            remediation.Result = "Executed";
            incident.Events.Add(new IncidentEvent { Message = "Engineer approved remediation" });
        }

        incident.Events.Add(new IncidentEvent { Message = "Remediation completed" });
        _db.AuditLogs.Add(new AuditLog { Action = "RemediationApproved", EntityType = "Incident", EntityId = incidentId.ToString() });
        await _db.SaveChangesAsync();
        return Ok(new { remediation.Status, remediation.Result });
    }
}
