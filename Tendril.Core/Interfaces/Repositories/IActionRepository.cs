using Tendril.Core.Domain.Entities;

public interface IActionRepository
{
    Task<List<ScraperAction>> GetByScraperIdAsync(Guid scraperId, CancellationToken cancellationToken = default);
    Task<ScraperAction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(ScraperAction action, CancellationToken cancellationToken = default);
    Task UpdateAsync(ScraperAction action, CancellationToken cancellationToken = default);
    Task UpdateRangeAsync(IEnumerable<ScraperAction> actions, CancellationToken cancellationToken = default);
    Task DeleteAsync(ScraperAction action, CancellationToken cancellationToken = default);
}
