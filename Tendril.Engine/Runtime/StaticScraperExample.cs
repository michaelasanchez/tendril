using System;
using System.Collections.Generic;
using System.Text;

namespace Tendril.Engine.Runtime;

using Tendril.Core.Domain.Entities;
using Tendril.Engine.Models;


public class StaticScraperExample : BaseScraper
{
    private readonly ScraperDefinition _def;

    public StaticScraperExample(ScraperDefinition def)
    {
        _def = def;
    }

    public override async Task<ScrapeResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Fake data until you plug in real logic
        await Task.Delay(200, cancellationToken);

        return Success(new List<RawScrapedEvent>
        {
            new RawScrapedEvent
            {
                Fields =
                {
                    ["Title"] = ["Fake Event"],
                    ["Date"] = ["2025-02-01"],
                    ["Time"] = ["7:30 PM"]
                }
            }
        });
    }
}
