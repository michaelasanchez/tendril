using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Core.Interfaces.Repositories;
using Tendril.Data.Models;

namespace Tendril.Data.Repositories;

public class EventRepository(TendrilDbContext _context) : IEventRepository
{
    private const int defaultLimit = 50;

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

        if (filter.Title is not null)
        {
            query = query.Where(x => x.Title.Contains(filter.Title));
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(x => x.StartUtc.Date >= filter.StartDate.Value.Date);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(x => x.StartUtc.Date <= filter.EndDate.Value.Date);
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
            .Where(x => x.Id == eventId);

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

    public async Task<List<Event>> GetByStatus(EventStatus status, CancellationToken ct = default)
    {
        return await _context.Events
            .Include(x => x.Category)
            .Include(x => x.Venue)
            .Where(x => x.Status == status)
            .AsNoTracking()
            .OrderBy(x => x.StartUtc)
            .ThenBy(x => x.ScrapedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<List<Event>> GetPotentialMatches(
        DateTime startWindow,
        DateTime endWindow,
        string pendingTitle,
        CancellationToken ct = default)
    {
        // 1. Narrow down by date window first (critical for DB index utilization)
        var query = _context.Events
            .Include(x => x.Category)
            .Include(x => x.Venue)
            .Where(x => x.Status == EventStatus.Published &&
                        x.StartUtc >= startWindow &&
                        x.StartUtc <= endWindow);

        var candidateEvents = await query.AsNoTracking().ToListAsync(ct);

        // 2. Perform fuzzy string matching in memory to check for variations
        // This protects DB performance while ensuring strings like "Rock Concert!" match "rock concert"
        var normalizedPendingTitle = NormalizeTitle(pendingTitle);

        return candidateEvents
            .Where(x => NormalizeTitle(x.Title).Contains(normalizedPendingTitle) ||
                        normalizedPendingTitle.Contains(NormalizeTitle(x.Title)))
            .ToList();
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

    public Task<Event?> Find(Event mappedEvent, CancellationToken ct = default)
    {
        return _context.Events
            .SingleOrDefaultAsync(x =>
                x.ScraperDefinitionId == mappedEvent.ScraperDefinitionId &&
                x.Title == mappedEvent.Title &&
                ((x.StartPrecision == DatePrecision.Day || mappedEvent.StartPrecision == DatePrecision.Day)
                    ? x.StartUtc.Date == mappedEvent.StartUtc.Date
                    : x.StartUtc == mappedEvent.StartUtc) &&
                x.Status != EventStatus.Suppressed, ct);
    }

    // Helper method to strip punctuation, spaces, and casing for cleaner soft-matching
    private string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return string.Empty;

        return new string(title.ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c))
            .ToArray());
    }

}
