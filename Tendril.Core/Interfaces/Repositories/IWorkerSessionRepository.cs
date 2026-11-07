using Tendril.Core.Domain.Entities;

namespace Tendril.Core.Interfaces.Repositories;

public interface IWorkerSessionRepository
{
    Task<WorkerSession?> GetLatestAsync(CancellationToken ct = default);
    Task StartAsync(WorkerSession session, CancellationToken ct = default);
    Task HeartbeatAsync(Guid sessionId, DateTimeOffset heartbeatAtUtc, CancellationToken ct = default);
    Task StopAsync(Guid sessionId, DateTimeOffset stoppedAtUtc, CancellationToken ct = default);
}