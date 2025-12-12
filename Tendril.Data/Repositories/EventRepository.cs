using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Data.Repositories;

public class EventRepository : IEventRepository
{
    private readonly TendrilDbContext _db;

    public EventRepository(TendrilDbContext db)
    {
        _db = db;
    }

    public async Task<List<Event>> GetAllAsync(DateTimeOffset? startDate, DateTimeOffset? endDate, CancellationToken cancellationToken = default)
    {
        var query = _db.Events
            .Include(x => x.Venue)
            .AsNoTracking();

        if (startDate.HasValue)
        {
            query = query.Where(x => x.StartUtc >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(x => x.StartUtc <= endDate.Value);
        }

        return await query
            .OrderBy(x => x.StartUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Event ev, CancellationToken cancellationToken = default)
    {
        _db.Events.Add(ev);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Event ev, CancellationToken cancellationToken = default)
    {
        _db.Events.Update(ev);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Event ev, CancellationToken cancellationToken = default)
    {
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> Exists(Event mappedEvent, CancellationToken cancellationToken = default)
    {
        return _db.Events
            .AsNoTracking()
            .AnyAsync(x =>
                x.ScraperDefinitionId == mappedEvent.ScraperDefinitionId &&
                x.Title == mappedEvent.Title &&
                x.StartUtc == mappedEvent.StartUtc);
    }

    public Task<Event?> Find(Event mappedEvent, CancellationToken cancellationToken = default)
    {
        return _db.Events
            .SingleOrDefaultAsync(x =>
                x.ScraperDefinitionId == mappedEvent.ScraperDefinitionId &&
                x.Title == mappedEvent.Title &&
                x.StartUtc.Date == mappedEvent.StartUtc.Date);
    }
}
