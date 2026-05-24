using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Data.Repositories;

public class AttemptHistoryRepository(TendrilDbContext db) : IAttemptHistoryRepository
{
    private const int defaultLimit = 50;

    public async Task Add(ScraperAttemptHistory attempt, CancellationToken cancellationToken = default)
    {
        db.AttemptHistory.Add(attempt);

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResponse<ScraperAttemptHistory>> GetPagedAttemptsAsync(
        Guid? scraperDefinitionId,
        int? limit,
        Guid? cursor,
        CancellationToken ct = default)
    {
        var query = db.AttemptHistory
            .Include(x => x.ScraperDefinition)
            .AsNoTracking();

        if (scraperDefinitionId.HasValue)
        {
            query = query.Where(x => x.ScraperDefinitionId == scraperDefinitionId.Value);
        }

        if (cursor.HasValue)
        {
            var reference = await db.AttemptHistory
                .Where(x => x.Id == cursor)
                .Select(x => new { x.StartTimeUtc, x.Id })
                .FirstOrDefaultAsync(ct);

            if (reference != null)
            {
                query = query.Where(x =>
                    x.StartTimeUtc < reference.StartTimeUtc ||
                    (x.StartTimeUtc == reference.StartTimeUtc && x.Id.CompareTo(reference.Id) > 0));
            }
        }

        var totalCount = await query.CountAsync(ct);
        var actualLimit = limit ?? defaultLimit;

        var results = await query
            .OrderByDescending(x => x.StartTimeUtc)
            .Take(actualLimit + 1)
            .ToListAsync(ct);

        var hasNextPage = results.Count > actualLimit;
        if (hasNextPage) results.RemoveAt(results.Count - 1);

        return new PagedResponse<ScraperAttemptHistory>
        {
            Items = results,
            NextCursor = hasNextPage ? results.LastOrDefault()?.Id : null,
            HasNextPage = hasNextPage,
            TotalCount = totalCount
        };
    }

    public async Task<ScraperAttemptHistory?> GetAttemptByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.AttemptHistory
            .Include(x => x.ScraperDefinition)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<List<ScraperAttemptHistory>> GetAttemptHistories(Guid scraperId, CancellationToken ct = default)
    {
        return db.AttemptHistory
            .Include(x => x.ScraperDefinition)
            .AsNoTracking()
            .Where(a => a.ScraperDefinitionId == scraperId)
            .OrderByDescending(a => a.StartTimeUtc)
            .ToListAsync(ct);
    }

    public async Task<List<EventRevision>> GetAuditTrailByEventAsync(Guid eventId, CancellationToken ct = default)
    {
        return await db.EventRevisions
            .Include(x => x.AttemptHistory)
            .Where(x => x.EventId == eventId)
            .OrderByDescending(x => x.ChangedAtUtc)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<PagedResponse<ScrapedEventRaw>> GetRawEventsByAttemptAsync(
        Guid attemptId,
        int? limit,
        Guid? cursor,
        CancellationToken ct = default)
    {
        var query = db.RawEvents
            .Where(x => x.ScraperAttemptHistoryId == attemptId)
            .AsNoTracking();

        var totalCount = await query.CountAsync(ct);
        var actualLimit = limit ?? defaultLimit;

        // Simple offset/id pagination for raw records
        if (cursor.HasValue)
        {
            query = query.Where(x => x.Id.CompareTo(cursor.Value) > 0);
        }

        var results = await query
            .OrderBy(x => x.Id)
            .Take(actualLimit + 1)
            .ToListAsync(ct);

        var hasNextPage = results.Count > actualLimit;
        if (hasNextPage) results.RemoveAt(results.Count - 1);

        return new PagedResponse<ScrapedEventRaw>
        {
            Items = results,
            NextCursor = hasNextPage ? results.LastOrDefault()?.Id : null,
            HasNextPage = hasNextPage,
            TotalCount = totalCount
        };
    }

    public async Task<List<EventRevision>> GetRevisionsByAttemptAsync(Guid attemptId, CancellationToken ct = default)
    {
        return await db.EventRevisions
            .Include(x => x.Event)
            .Include(x => x.RawEvent)
            .Where(x => x.AttemptHistoryId == attemptId)
            .OrderBy(x => x.ChangedAtUtc)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task UpdateAsync(ScraperAttemptHistory attempt, CancellationToken ct = default)
    {
        db.AttemptHistory.Update(attempt);

        await db.SaveChangesAsync(ct);
    }
}