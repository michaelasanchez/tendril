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

    public async Task<Event?> Find(Event mappedEvent, CancellationToken cancellationToken = default)
    {
        var startUtc = mappedEvent.StartUtc.Date;

        var test = await _db.Events.Where(x => x.StartUtc.Date == startUtc).ToListAsync();

        var test2 = await _db.Events.Where(x => x.Title == mappedEvent.Title).ToListAsync();

        if (test2 is not null)
        {
            var yup = test2.FirstOrDefault();

            var test3 = yup.StartUtc.Date == mappedEvent.StartUtc.Date;
        }

        return await _db.Events
            .SingleOrDefaultAsync(x =>
                x.ScraperDefinitionId == mappedEvent.ScraperDefinitionId &&
                x.Title == mappedEvent.Title &&
                x.StartUtc.Date == mappedEvent.StartUtc.Date);
    }
}
