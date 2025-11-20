using System;
using System.Collections.Generic;
using System.Text;
using Tendril.Core.Domain.Entities;

namespace Tendril.Core.Interfaces.Repositories;

public interface IScraperRepository
{
    Task<List<ScraperDefinition>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ScraperDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ScraperDefinition?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(ScraperDefinition scraper, CancellationToken cancellationToken = default);
    Task UpdateAsync(ScraperDefinition scraper, CancellationToken cancellationToken = default);
    Task DeleteAsync(ScraperDefinition scraper, CancellationToken cancellationToken = default);

    // Used by Worker
    Task<List<ScraperDefinition>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
}