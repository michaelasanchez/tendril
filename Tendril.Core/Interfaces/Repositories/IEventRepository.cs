namespace Tendril.Core.Interfaces.Repositories;

using Tendril.Core.Domain.Entities;

public interface IEventRepository
{
    Task<List<Event>> GetAllAsync(DateTimeOffset? startDate, DateTimeOffset? endDate, CancellationToken ct = default);
    Task<Event?> GetById(Guid eventId, CancellationToken ct = default);
    Task<List<Event>> GetByScraperIdAsync(Guid id, DateTimeOffset? startDate, DateTimeOffset? endDate, CancellationToken ct = default);
    Task AddAsync(Event ev, CancellationToken ct = default);
    Task UpdateAsync(Event ev, CancellationToken ct = default);
    Task DeleteAsync(Event ev, CancellationToken ct = default);
    Task<bool> Exists(Event mappedEvent, CancellationToken ct = default);
    Task<Event?> Find(Event mappedEvent, CancellationToken ct = default);
}
