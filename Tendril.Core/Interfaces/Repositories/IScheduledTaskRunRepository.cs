using Tendril.Core.Domain.Entities;

namespace Tendril.Core.Interfaces.Repositories;

public interface IScheduledTaskRunRepository
{
    // Management Methods
    Task<ScheduledTaskRun?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(ScheduledTaskRun run, CancellationToken ct = default);
    Task UpdateAsync(ScheduledTaskRun run, CancellationToken ct = default);
    Task DeleteAsync(ScheduledTaskRun run, CancellationToken ct = default);

    // Worker Specific Methods
    Task<ScheduledTaskRun?> GetIncompleteRunAsync(Guid scheduledTaskId, CancellationToken ct = default);
    Task<ScheduledTaskRun?> GetByIdWithAttemptsAsync(Guid id, CancellationToken ct = default);
}
