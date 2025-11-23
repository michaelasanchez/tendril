using Tendril.Core.Domain.Entities;
using Tendril.Engine.Abstractions;
using Tendril.Engine.Models;

namespace Tendril.Engine.Runtime;

public class ScrapeExecutor : IScrapeExecutor
{
    private readonly IScraperFactory _factory;

    public ScrapeExecutor(IScraperFactory factory)
    {
        _factory = factory;
    }

    public async Task<ScrapeResult> RunScraperAsync(
        ScraperDefinition scraperDef,
        bool selectorsOnly = false,
        CancellationToken cancellationToken = default)
    {
        var scraper = _factory.CreateScraper(scraperDef);

        return await scraper.ExecuteAsync(selectorsOnly, cancellationToken);
    }
}
