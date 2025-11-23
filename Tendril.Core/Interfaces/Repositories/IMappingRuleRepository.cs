using Tendril.Core.Domain.Entities;

namespace Tendril.Core.Interfaces.Repositories;

public interface IMappingRuleRepository
{
    Task<List<ScraperMappingRule>> GetByScraperIdAsync(Guid scraperId, CancellationToken cancellationToken = default);
    Task<ScraperMappingRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(ScraperMappingRule rule, CancellationToken cancellationToken = default);
    Task UpdateAsync(ScraperMappingRule rule, CancellationToken cancellationToken = default);
    Task DeleteAsync(ScraperMappingRule rule, CancellationToken cancellationToken = default);
}
