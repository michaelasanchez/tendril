using Tendril.Core.Domain.Entities;
using Tendril.Engine.Models;

namespace Tendril.Engine.Abstractions;

public interface IScrapeExecutor
{
    Task<ScrapeResult> RunScraperAsync(
        ScraperDefinition scraperDef,
        CancellationToken cancellationToken = default);
}
