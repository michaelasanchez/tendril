namespace Tendril.Core.Interfaces.Repositories;

using Tendril.Core.Domain.Entities;

public interface IEventRepository
{
    Task<List<Event>> GetAllAsync(DateTimeOffset? startDate, DateTimeOffset? endDate, CancellationToken cancellationToken = default);
    Task AddAsync(Event ev, CancellationToken cancellationToken = default);
    Task UpdateAsync(Event ev, CancellationToken cancellationToken = default);
    Task DeleteAsync(Event ev, CancellationToken cancellationToken = default);
    Task<bool> Exists(Event mappedEvent, CancellationToken cancellationToken = default);
    Task<Event?> Find(Event mappedEvent, CancellationToken cancellationToken = default);
}
