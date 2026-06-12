using Tendril.Core.Domain;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Data.Models;

namespace Tendril.Core.Interfaces.Repositories;

public interface IEventRepository
{
    Task<PagedResponse<Event>> GetAllAsync(EventFilter filter, int? limit, Guid? cursor, CancellationToken ct = default);
    Task<Event?> GetById(Guid eventId, CancellationToken ct = default);
    Task<List<Event>> GetByScraperIdAsync(Guid id, DateTimeOffset? startDate, DateTimeOffset? endDate, CancellationToken ct = default);
    Task<List<Event>> GetByStatus(EventStatus status, CancellationToken ct = default);
    Task AddAsync(Event ev, CancellationToken ct = default);
    Task UpdateAsync(Event ev, CancellationToken ct = default);
    Task DeleteAsync(Event ev, CancellationToken ct = default);
    Task<Event?> Find(Event mappedEvent, CancellationToken ct = default);
}
