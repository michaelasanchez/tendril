using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Core.Interfaces.Repositories;
using Tendril.Data.Models;

namespace Tendril.Data.Repositories;

public class EventRepository(TendrilDbContext _context) : IEventRepository
{
    private const int defaultLimit = 5;

    public async Task<PagedResponse<Event>> GetAllAsync(
        EventFilter filter,
        int? limit,
        Guid? cursor,
        CancellationToken ct = default)
    {
        var query = _context.Events
            .Include(x => x.Category)
            .Include(x => x.Venue)
            .Where(x => x.Status != EventStatus.Suppressed)
            .AsNoTracking();

        if (filter.StartDate.HasValue)
        {
            query = query.Where(x => x.StartUtc >= filter.StartDate);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(x => x.StartUtc <= filter.EndDate);
        }

        if (filter.CategoryIds is { Count: > 0 })
        {
            query = query.Where(x => filter.CategoryIds!.Contains(x.CategoryId!.Value));
        }

        if (filter.VenueIds is { Count: > 0 })
        {
            query = query.Where(x => filter.VenueIds!.Contains(x.VenueId!.Value));
        }

        if (cursor.HasValue)
        {
            var reference = await _context.Events
                .Where(e => e.Id == cursor)
                .Select(e => new { e.StartUtc, e.Id })
                .FirstOrDefaultAsync(ct);

            if (reference != null)
            {
                query = query.Where(e =>
                    e.StartUtc > reference.StartUtc ||
                    (e.StartUtc == reference.StartUtc && e.Id.CompareTo(reference.Id) > 0));
            }
        }

        var totalCount = await query.CountAsync();

        var actualLimit = limit ?? defaultLimit;

        var results = await query
            .OrderBy(e => e.StartUtc)
            .ThenBy(e => e.Id)
            .Take(actualLimit + 1)
            .ToListAsync(ct);

        var hasNextPage = results.Count > actualLimit;

        if (hasNextPage)
        {
            results.RemoveAt(results.Count - 1);
        }

        return new PagedResponse<Event>
        {
            Items = results,
            NextCursor = hasNextPage ? results.LastOrDefault()?.Id : null,
            HasNextPage = hasNextPage,
            TotalCount = totalCount
        };
    }

    public async Task<Event?> GetById(Guid eventId, CancellationToken ct = default)
    {
        var query = _context.Events
            .Include(x => x.Category)
            .Include(x => x.Venue)
            .Where(x => x.Id == eventId)
            .AsNoTracking();

        return await query.FirstOrDefaultAsync(ct);
    }

    public async Task<List<Event>> GetByScraperIdAsync(Guid id, DateTimeOffset? startDate, DateTimeOffset? endDate, CancellationToken ct = default)
    {
        var query = _context.Events
            .Include(x => x.Category)
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
        _context.Events.Add(ev);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Event ev, CancellationToken ct = default)
    {
        _context.Events.Update(ev);

        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Event ev, CancellationToken ct = default)
    {
        _context.Events.Remove(ev);
        await _context.SaveChangesAsync(ct);
    }

    public Task<bool> Exists(Event mappedEvent, CancellationToken ct = default)
    {
        return _context.Events
            .AsNoTracking()
            .AnyAsync(x =>
                x.ScraperDefinitionId == mappedEvent.ScraperDefinitionId &&
                x.Title == mappedEvent.Title &&
                x.StartUtc == mappedEvent.StartUtc &&
                x.Status != EventStatus.Suppressed, ct);
    }

    public Task<Event?> Find(Event mappedEvent, CancellationToken ct = default)
    {
        return _context.Events
            .SingleOrDefaultAsync(x =>
                x.ScraperDefinitionId == mappedEvent.ScraperDefinitionId &&
                x.Title == mappedEvent.Title &&
                x.StartUtc == mappedEvent.StartUtc &&
                x.Status != EventStatus.Suppressed, ct);
    }
}
