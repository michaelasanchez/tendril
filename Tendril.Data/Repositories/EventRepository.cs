using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Data.Repositories;

public class EventRepository(TendrilDbContext db) : IEventRepository
{
    public async Task<List<Event>> GetAllAsync(DateTimeOffset? startDate, DateTimeOffset? endDate, CancellationToken ct = default)
    {
        var query = db.Events
            .Include(x => x.Venue)
            .Where(x => x.Status != EventStatus.Suppressed)
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
            .ToListAsync(ct);
    }

    public async Task<Event?> GetById(Guid eventId, CancellationToken ct = default)
    {
        var query = db.Events
            .Include(x => x.Venue)
            .Where(x => x.Id == eventId)
            .AsNoTracking();

        return await query.FirstOrDefaultAsync(ct);
    }

    public async Task<List<Event>> GetByScraperIdAsync(Guid id, DateTimeOffset? startDate, DateTimeOffset? endDate, CancellationToken ct = default)
    {
        var query = db.Events
            .Include(x => x.Venue)
            .Where(x => x.ScraperDefinitionId == id)
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
            .ThenBy(x => x.ScrapedAtUtc)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Event ev, CancellationToken ct = default)
    {
        db.Events.Add(ev);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Event ev, CancellationToken ct = default)
    {
        db.Events.Update(ev);

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Event ev, CancellationToken ct = default)
    {
        db.Events.Remove(ev);
        await db.SaveChangesAsync(ct);
    }

    public Task<bool> Exists(Event mappedEvent, CancellationToken ct = default)
    {
        return db.Events
            .AsNoTracking()
            .AnyAsync(x =>
                x.ScraperDefinitionId == mappedEvent.ScraperDefinitionId &&
                x.Title == mappedEvent.Title &&
                x.StartUtc == mappedEvent.StartUtc &&
                x.Status != EventStatus.Suppressed, ct);
    }

    public Task<Event?> Find(Event mappedEvent, CancellationToken ct = default)
    {
        return db.Events
            .SingleOrDefaultAsync(x =>
                x.ScraperDefinitionId == mappedEvent.ScraperDefinitionId &&
                x.Title == mappedEvent.Title &&
                x.StartUtc == mappedEvent.StartUtc &&
                x.Status != EventStatus.Suppressed, ct);
    }
}
