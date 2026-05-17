using Tendril.Core.Domain.Entities;

namespace Tendril.Core.Interfaces.Repositories;

public interface IScheduledTaskRepository
{
    Task<List<ScheduledTask>> GetAllAsync(CancellationToken ct = default);
    Task<ScheduledTask?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ScheduledTask?> GetByIdWithScrapersAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(ScheduledTask task, CancellationToken ct = default);
    Task UpdateAsync(ScheduledTask task, CancellationToken ct = default);
    Task DeleteAsync(ScheduledTask task, CancellationToken ct = default);

    Task<ScheduledTask?> GetNextDueTaskAsync(DateTimeOffset now, CancellationToken ct = default);
    Task<List<ScraperDefinition>> GetScrapersForTaskAsync(ScheduledTask task, CancellationToken ct = default);
}