using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Data.Repositories;

public class ScheduledTaskRepository(TendrilDbContext db) : IScheduledTaskRepository
{
    #region Management Methods (CRUD)

    public async Task<List<ScheduledTask>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.ScheduledTasks
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<ScheduledTask?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.ScheduledTasks
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    /// <summary>
    /// Fetches a single task along with its explicitly mapped scrapers. 
    /// Useful for the administration/editor UI.
    /// </summary>
    public async Task<ScheduledTask?> GetByIdWithScrapersAsync(Guid id, CancellationToken ct = default)
    {
        return await db.ScheduledTasks
            .Include(t => t.ScraperDefinitions)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task AddAsync(ScheduledTask task, CancellationToken ct = default)
    {
        db.ScheduledTasks.Add(task);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ScheduledTask task, CancellationToken ct = default)
    {
        db.ScheduledTasks.Update(task);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(ScheduledTask task, CancellationToken ct = default)
    {
        db.ScheduledTasks.Remove(task);
        await db.SaveChangesAsync(ct);
    }

    #endregion

    #region Worker Specific Methods

    public Task<ScheduledTaskSnapshot?> GetSnapshotAsync(Guid id, CancellationToken ct = default) =>
        db.ScheduledTasks
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new ScheduledTaskSnapshot(
                t.Id, t.Name, t.IsDisabled, t.CronExpression, t.SelectionStrategy, t.MaxRetries))
            .FirstOrDefaultAsync(ct);

    public Task<List<Guid>> GetDueTaskIdsAsync(DateTimeOffset now, CancellationToken ct = default) =>
        db.ScheduledTasks
            .AsNoTracking()
            .Where(t => !t.IsDisabled
                        && (t.Status == ScheduledTaskStatus.Idle || t.Status == ScheduledTaskStatus.Retrying)
                        && t.NextRunAtUtc <= now)
            .OrderBy(t => t.NextRunAtUtc)
            .Select(t => t.Id)
            .ToListAsync(ct);

    public Task<List<Guid>> GetEventFeedScraperIdsAsync(
        Guid taskId, SelectionStrategy strategy, CancellationToken ct = default)
    {
        IQueryable<ScraperDefinition> scrapers;

        switch (strategy)
        {
            case SelectionStrategy.All:
                scrapers = db.Scrapers.AsNoTracking();
                break;

            case SelectionStrategy.Selected:
                scrapers = db.ScheduledTasks
                    .AsNoTracking()
                    .Where(t => t.Id == taskId)
                    .SelectMany(t => t.ScraperDefinitions);
                break;

            default:
                return Task.FromResult(new List<Guid>());
        }

        return scrapers
            .Where(s => s.IsEventFeed)
            .OrderBy(s => s.Name)
            .Select(s => s.Id)
            .ToListAsync(ct);
    }

    // Safe only because exactly one worker process ever runs (enforced by SingleInstanceGuard).
    public Task<int> RecoverOrphanedRunningTasksAsync(DateTimeOffset now, CancellationToken ct = default) =>
        db.ScheduledTasks
            .Where(t => t.Status == ScheduledTaskStatus.Running)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Status, ScheduledTaskStatus.Retrying)
                .SetProperty(t => t.NextRunAtUtc, now), ct);

    public Task MarkRunStartedAsync(Guid id, DateTimeOffset startedAtUtc, CancellationToken ct = default) =>
        db.ScheduledTasks
            .Where(t => t.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Status, ScheduledTaskStatus.Running)
                .SetProperty(t => t.LastRunStartedAtUtc, (DateTimeOffset?)startedAtUtc), ct);

    public Task SaveScheduleAsync(
        Guid id, ScheduledTaskStatus status, DateTimeOffset nextRunAtUtc,
        DateTimeOffset completedAtUtc, CancellationToken ct = default) =>
        db.ScheduledTasks
            .Where(t => t.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Status, status)
                .SetProperty(t => t.NextRunAtUtc, nextRunAtUtc)
                .SetProperty(t => t.LastRunCompletedAtUtc, (DateTimeOffset?)completedAtUtc), ct);

    public Task SetStatusAsync(Guid id, ScheduledTaskStatus status, CancellationToken ct = default) =>
        db.ScheduledTasks
            .Where(t => t.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, status), ct);

    public Task DisableAsync(
        Guid id, ScheduledTaskStatus status, DateTimeOffset completedAtUtc, CancellationToken ct = default) =>
        db.ScheduledTasks
            .Where(t => t.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.IsDisabled, true)
                .SetProperty(t => t.Status, status)
                .SetProperty(t => t.LastRunCompletedAtUtc, (DateTimeOffset?)completedAtUtc), ct);

    #endregion
}
