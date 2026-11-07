using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Data.Repositories;

public class ScheduledTaskRunRepository(TendrilDbContext db) : IScheduledTaskRunRepository
{
    #region Management Methods (CRUD)

    public async Task<ScheduledTaskRun?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.ScheduledTaskRuns
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task AddAsync(ScheduledTaskRun run, CancellationToken ct = default)
    {
        db.ScheduledTaskRuns.Add(run);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ScheduledTaskRun run, CancellationToken ct = default)
    {
        db.ScheduledTaskRuns.Update(run);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(ScheduledTaskRun run, CancellationToken ct = default)
    {
        db.ScheduledTaskRuns.Remove(run);
        await db.SaveChangesAsync(ct);
    }

    #endregion

    #region Worker Specific Methods

    /// <summary>
    /// Locates an active or retrying run for a specific task. 
    /// Ensures we resume a failed batch rather than starting over.
    /// </summary>
    public async Task<ScheduledTaskRun?> GetIncompleteRunAsync(Guid scheduledTaskId, CancellationToken ct = default)
    {
        // Only runs that are still in progress. A Failed run is finished: resuming it would skip
        // every scraper that succeeded back then and fail again without any retries.
        // AttemptHistories are deliberately not loaded; use GetSucceededScraperIdsAsync.
        return await db.ScheduledTaskRuns
            .Where(r => r.ScheduledTaskId == scheduledTaskId
                        && (r.Status == RunStatus.Running || r.Status == RunStatus.Retrying))
            .OrderByDescending(r => r.StartTimeUtc)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Fetches a task run along with all historical attempts made during this run.
    /// Used by the worker to evaluate which scrapers succeeded and which failed.
    /// </summary>
    public async Task<ScheduledTaskRun?> GetByIdWithAttemptsAsync(Guid id, CancellationToken ct = default)
    {
        return await db.ScheduledTaskRuns
            .Include(r => r.AttemptHistories)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public Task<List<ScheduledTaskRun>> GetRecentAsync(int limit, CancellationToken ct = default) =>
        db.ScheduledTaskRuns
            .AsNoTracking()
            .Include(run => run.ScheduledTask)
            .Include(run => run.AttemptHistories)
            .OrderByDescending(run => run.StartTimeUtc)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<HashSet<Guid>> GetSucceededScraperIdsAsync(Guid runId, CancellationToken ct = default)
    {
        var ids = await db.ScheduledTaskRuns
            .AsNoTracking()
            .Where(r => r.Id == runId)
            .SelectMany(r => r.AttemptHistories)
            .Where(a => a.Success)
            .Select(a => a.ScraperDefinitionId)
            .Distinct()
            .ToListAsync(ct);

        return [.. ids];
    }

    public Task<int> CloseInterruptedAttemptsAsync(DateTimeOffset now, CancellationToken ct = default) =>
        db.AttemptHistory
            .Where(a => a.ScheduledTaskRunId != null && a.ErrorMessage == "Running...")
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.Success, false)
                .SetProperty(a => a.EndTimeUtc, now)
                .SetProperty(a => a.ErrorMessage, (string?)"Interrupted: the worker stopped before this attempt finished."), ct);

    #endregion
}