using Tendril.Core.Domain.Entities;

public interface IActionRepository
{
    Task<List<ScraperAction>> GetByScraperIdAsync(Guid scraperId, CancellationToken cancellationToken = default);
    Task<ScraperAction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(ScraperAction selector, CancellationToken cancellationToken = default);
    Task UpdateAsync(ScraperAction selector, CancellationToken cancellationToken = default);
    Task DeleteAsync(ScraperAction selector, CancellationToken cancellationToken = default);
}
