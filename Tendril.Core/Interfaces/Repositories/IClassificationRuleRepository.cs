using Tendril.Core.Domain.Entities;

namespace Tendril.Core.Interfaces.Repositories;

public interface IClassificationRuleRepository
{
    Task<List<ScraperClassificationRule>> GetByScraperIdAsync(Guid scraperId, CancellationToken cancellationToken = default);
    Task<ScraperClassificationRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(ScraperClassificationRule rule, CancellationToken cancellationToken = default);
    Task UpdateAsync(ScraperClassificationRule rule, CancellationToken cancellationToken = default);
    Task DeleteAsync(ScraperClassificationRule rule, CancellationToken cancellationToken = default);
}
