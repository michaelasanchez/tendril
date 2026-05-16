using Tendril.Core.Domain;
using Tendril.Core.Domain.Entities;

namespace Tendril.Core.Interfaces.Repositories;

public interface IAttemptHistoryRepository
{
    Task Add(ScraperAttemptHistory attempt, CancellationToken ct = default);

    Task<List<ScraperAttemptHistory>> GetAttemptHistories(Guid scraperId, CancellationToken ct = default);
    Task UpdateAsync(ScraperAttemptHistory attempt, CancellationToken ct = default);


    Task<PagedResponse<ScraperAttemptHistory>> GetPagedAttemptsAsync(
        Guid? scraperDefinitionId,
        int? limit,
        Guid? cursor,
        CancellationToken ct = default);

    Task<ScraperAttemptHistory?> GetAttemptByIdAsync(Guid id, CancellationToken ct = default);

    Task<PagedResponse<ScrapedEventRaw>> GetRawEventsByAttemptAsync(
        Guid attemptId,
        int? limit,
        Guid? cursor,
        CancellationToken ct = default);

    Task<List<EventRevision>> GetRevisionsByAttemptAsync(Guid attemptId, CancellationToken ct = default);

    Task<List<EventRevision>> GetAuditTrailByEventAsync(Guid eventId, CancellationToken ct = default);
}