using Tendril.Core.Domain;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;

namespace Tendril.Core.Interfaces.Repositories;

public interface IScheduledTaskRepository
{
    Task<List<ScheduledTask>> GetAllAsync(CancellationToken ct = default);
    Task<ScheduledTask?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ScheduledTask?> GetByIdWithScrapersAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(ScheduledTask task, CancellationToken ct = default);
    Task UpdateAsync(ScheduledTask task, CancellationToken ct = default);
    Task DeleteAsync(ScheduledTask task, CancellationToken ct = default);

    // ---- Worker-specific -------------------------------------------------------
    // The worker reads snapshots and writes targeted updates (ExecuteUpdate), so it never
    // overwrites UI edits and a RowVersion conflict can't strand a task in Running.

    Task<ScheduledTaskSnapshot?> GetSnapshotAsync(Guid id, CancellationToken ct = default);
    Task<List<Guid>> GetDueTaskIdsAsync(DateTimeOffset now, CancellationToken ct = default);

    /// <summary>Ids of the event-feed scrapers a task should run, per its selection strategy.</summary>
    Task<List<Guid>> GetEventFeedScraperIdsAsync(Guid taskId, SelectionStrategy strategy, CancellationToken ct = default);

    /// <summary>Startup recovery: tasks left Running by a crash become Retrying and due now.</summary>
    Task<int> RecoverOrphanedRunningTasksAsync(DateTimeOffset now, CancellationToken ct = default);

    Task MarkRunStartedAsync(Guid id, DateTimeOffset startedAtUtc, CancellationToken ct = default);
    Task SaveScheduleAsync(Guid id, ScheduledTaskStatus status, DateTimeOffset nextRunAtUtc, DateTimeOffset completedAtUtc, CancellationToken ct = default);
    Task SetStatusAsync(Guid id, ScheduledTaskStatus status, CancellationToken ct = default);
    Task DisableAsync(Guid id, ScheduledTaskStatus status, DateTimeOffset completedAtUtc, CancellationToken ct = default);
}