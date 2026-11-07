using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Data.Repositories;

public class WorkerSessionRepository(TendrilDbContext db) : IWorkerSessionRepository
{
    public Task<WorkerSession?> GetLatestAsync(CancellationToken ct = default) =>
        db.Set<WorkerSession>()
            .AsNoTracking()
            .OrderByDescending(session => session.StartedAtUtc)
            .FirstOrDefaultAsync(ct);

    public async Task StartAsync(WorkerSession session, CancellationToken ct = default)
    {
        db.Set<WorkerSession>().Add(session);
        await db.SaveChangesAsync(ct);
    }

    public Task HeartbeatAsync(Guid sessionId, DateTimeOffset heartbeatAtUtc, CancellationToken ct = default) =>
        db.Set<WorkerSession>()
            .Where(session => session.Id == sessionId)
            .ExecuteUpdateAsync(update => update
                .SetProperty(session => session.LastHeartbeatAtUtc, heartbeatAtUtc), ct);

    public Task StopAsync(Guid sessionId, DateTimeOffset stoppedAtUtc, CancellationToken ct = default) =>
        db.Set<WorkerSession>()
            .Where(session => session.Id == sessionId)
            .ExecuteUpdateAsync(update => update
                .SetProperty(session => session.StoppedAtUtc, (DateTimeOffset?)stoppedAtUtc)
                .SetProperty(session => session.LastHeartbeatAtUtc, stoppedAtUtc), ct);
}