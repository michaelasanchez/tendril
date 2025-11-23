using Tendril.Core.Domain.Entities;

namespace Tendril.Core.Interfaces.Repositories;

public interface IRawEventRepository
{
    Task AddAsync(ScrapedEventRaw rawEvent, CancellationToken cancellationToken = default);

    Task<ScrapedEventRaw?> GetMostRecentForScraperAsync(
        Guid scraperId,
        CancellationToken cancellationToken = default);

    Task<List<ScrapedEventRaw>> GetForScraperAsync(
        Guid scraperId,
        int take = 50,
        CancellationToken cancellationToken = default);
}
