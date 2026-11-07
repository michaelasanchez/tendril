using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Api.Controllers;

[ApiController]
[Route("worker")]
[Authorize]
public sealed class WorkerStatusController(IWorkerSessionRepository sessions) : ControllerBase
{
    private static readonly TimeSpan HeartbeatTimeout = TimeSpan.FromSeconds(90);

    [HttpGet("status")]
    public async Task<ActionResult<WorkerStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        var session = await sessions.GetLatestAsync(cancellationToken);
        if (session is null)
        {
            return Ok(new WorkerStatusDto(
                "Unknown", false, null, null, null, null, null, null, null));
        }

        var now = DateTimeOffset.UtcNow;
        var heartbeatAgeSeconds = Math.Max(0, (int)(now - session.LastHeartbeatAtUtc).TotalSeconds);
        var isOnline = session.StoppedAtUtc is null && now - session.LastHeartbeatAtUtc <= HeartbeatTimeout;
        var status = session.StoppedAtUtc is not null
            ? "Stopped"
            : isOnline ? "Online" : "Offline";

        return Ok(new WorkerStatusDto(
            status,
            isOnline,
            session.Id,
            session.MachineName,
            session.ProcessId,
            session.StartedAtUtc,
            session.LastHeartbeatAtUtc,
            session.StoppedAtUtc,
            heartbeatAgeSeconds));
    }
}