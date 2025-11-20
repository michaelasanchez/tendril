using Tendril.Core.Domain.Entities;

public interface ISelectorRepository
{
    Task<List<ScraperSelector>> GetByScraperIdAsync(Guid scraperId, CancellationToken cancellationToken = default);
    Task<ScraperSelector?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(ScraperSelector selector, CancellationToken cancellationToken = default);
    Task UpdateAsync(ScraperSelector selector, CancellationToken cancellationToken = default);
    Task DeleteAsync(ScraperSelector selector, CancellationToken cancellationToken = default);
}
