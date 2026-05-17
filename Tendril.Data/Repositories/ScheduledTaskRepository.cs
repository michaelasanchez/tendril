using Microsoft.EntityFrameworkCore;
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

    /// <summary>
    /// Locates the oldest pending task that is due for execution.
    /// </summary>
    public async Task<ScheduledTask?> GetNextDueTaskAsync(DateTimeOffset now, CancellationToken ct = default)
    {
        return await db.ScheduledTasks
            .Where(t => !t.IsDisabled && t.Status == "Idle" && t.NextRunAtUtc <= now)
            .OrderBy(t => t.NextRunAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Resolves the collection of active scrapers assigned to run under this task, 
    /// evaluated against the task's SelectionStrategy.
    /// </summary>
    public async Task<List<ScraperDefinition>> GetScrapersForTaskAsync(ScheduledTask task, CancellationToken ct = default)
    {
        // Strategy: Run all active scrapers in the entire platform
        if (task.SelectionStrategy == SelectionStrategy.All)
        {
            return await db.Scrapers
                .AsNoTracking()
                .ToListAsync(ct);
        }

        // Strategy: Run only the specific scrapers mapped via the associative table
        return await db.ScheduledTasks
            .Where(t => t.Id == task.Id)
            .SelectMany(t => t.ScraperDefinitions)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    #endregion
}