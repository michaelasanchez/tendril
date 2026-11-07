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
    Task<List<ScheduledTaskRun>> GetRecentAsync(int limit, CancellationToken ct = default);

    /// <summary>
    /// Scrapers with at least one successful attempt in the run. Always reads fresh from the
    /// database, because attempts are written by other (per-scraper) contexts.
    /// </summary>
    Task<HashSet<Guid>> GetSucceededScraperIdsAsync(Guid runId, CancellationToken ct = default);

    /// <summary>
    /// Startup recovery: closes task-run attempts that were still "Running..." when the worker
    /// died. Attempts without a task run (API test runs) are left alone.
    /// </summary>
    Task<int> CloseInterruptedAttemptsAsync(DateTimeOffset now, CancellationToken ct = default);
}
