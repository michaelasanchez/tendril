using System;
using System.Collections.Generic;
using System.Text;

namespace Tendril.Engine.Runtime;

using Tendril.Core.Domain.Entities;
using Tendril.Engine.Abstractions;
using Tendril.Engine.Models;

public class ScrapeExecutor : IScrapeExecutor
{
    private readonly IScraperFactory _factory;

    public ScrapeExecutor(IScraperFactory factory)
    {
        _factory = factory;
    }

    public async Task<ScrapeResult> RunScraperAsync(
        ScraperDefinition scraperDef,
        CancellationToken cancellationToken)
    {
        var scraper = _factory.CreateScraper(scraperDef);
        return await scraper.ExecuteAsync(cancellationToken);
    }
}
