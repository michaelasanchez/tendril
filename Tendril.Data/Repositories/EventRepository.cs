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
    private const double titleSimilarityWeight = 0.5;
    private const double venueMatchWeight = 0.2;
    private const double startMatchWeight = 0.2;
    private const double endMatchWeight = 0.1;
    private const double potentialMatchThreshold = 0.65;

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
        Event pendingEvent,
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

        // 2. Perform token-based similarity matching in memory
        return candidateEvents
            .Where(x => CalculateMatchScore(x, pendingEvent) >= potentialMatchThreshold)
            .ToList();
    }

    private double CalculateMatchScore(Event candidate, Event pendingEvent)
    {
        var weightedScore = AreTitlesSimilar(candidate.Title, pendingEvent.Title) * titleSimilarityWeight;
        var totalWeight = titleSimilarityWeight;

        if (candidate.VenueId.HasValue && pendingEvent.VenueId.HasValue)
        {
            weightedScore += (candidate.VenueId == pendingEvent.VenueId ? 1 : 0) * venueMatchWeight;
            totalWeight += venueMatchWeight;
        }

        weightedScore += (HaveSameStart(candidate, pendingEvent) ? 1 : 0) * startMatchWeight;
        totalWeight += startMatchWeight;

        if (candidate.EndUtc.HasValue && pendingEvent.EndUtc.HasValue)
        {
            weightedScore += (HaveSameEnd(candidate, pendingEvent) ? 1 : 0) * endMatchWeight;
            totalWeight += endMatchWeight;
        }

        return weightedScore / totalWeight;
    }

    private bool HaveSameStart(Event first, Event second)
    {
        return first.StartPrecision == DatePrecision.Day || second.StartPrecision == DatePrecision.Day
            ? first.StartUtc.Date == second.StartUtc.Date
            : first.StartUtc == second.StartUtc;
    }

    private bool HaveSameEnd(Event first, Event second)
    {
        return first.EndPrecision != DatePrecision.Minute || second.EndPrecision != DatePrecision.Minute
            ? first.EndUtc!.Value.Date == second.EndUtc!.Value.Date
            : first.EndUtc == second.EndUtc;
    }

    private double AreTitlesSimilar(string title1, string title2)
    {
        var tokens1 = Tokenize(title1);
        var tokens2 = Tokenize(title2);

        if (tokens1.Count == 0 || tokens2.Count == 0) return 0;

        int intersection = tokens1.Intersect(tokens2).Count();
        int union = tokens1.Union(tokens2).Count();

        return (double)intersection / union;
    }

    private HashSet<string> Tokenize(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return new HashSet<string>();

        // Common noise words in event lineups to ignore
        var stopWords = new HashSet<string> { "ft", "feat", "with", "and", "the", "at" };

        // Extracts continuous alphanumeric words, dropping spaces, punctuation, and symbols (+, -, .)
        var matches = System.Text.RegularExpressions.Regex.Matches(title.ToLower(), @"[a-z0-9]+");

        return matches
            .Cast<System.Text.RegularExpressions.Match>()
            .Select(m => m.Value)
            .Where(word => word.Length > 1 && !stopWords.Contains(word)) // Ignore single-char noise & stop words
            .ToHashSet();
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
                (
                    x.Title == mappedEvent.Title ||
                    x.Title.Contains(mappedEvent.Title) ||
                    mappedEvent.Title.Contains(x.Title)
                ) &&
                ((x.StartPrecision == DatePrecision.Day || mappedEvent.StartPrecision == DatePrecision.Day)
                    ? x.StartUtc.Date == mappedEvent.StartUtc.Date
                    : x.StartUtc == mappedEvent.StartUtc) &&
                x.Status != EventStatus.Suppressed, ct);
    }
}
