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
        return await db.ScheduledTaskRuns
            // You can adjust the string literals to Enums later if you prefer
            .Where(r => r.ScheduledTaskId == scheduledTaskId && r.Status != RunStatus.Completed)
            .OrderByDescending(r => r.StartTimeUtc) // Grab the most recent incomplete one
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

    #endregion
}