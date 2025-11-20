namespace Tendril.Core.Interfaces.Repositories;

using Tendril.Core.Domain.Entities;

public interface IEventRepository
{
    Task<List<Event>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Event ev, CancellationToken cancellationToken = default);
    Task UpdateAsync(Event ev, CancellationToken cancellationToken = default);
    Task DeleteAsync(Event ev, CancellationToken cancellationToken = default);
}
