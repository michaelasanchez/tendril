using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Entities;
using Tendril.Data;
using Tendril.Engine.Models;
using Tendril.Engine.Playwright;

namespace Tendril.Engine.Runtime;

public class DynamicScraper : BaseScraper
{
    private readonly ScraperDefinition _def;
    private readonly TendrilDbContext _db;

    public DynamicScraper(ScraperDefinition def, TendrilDbContext db)
    {
        _def = def;
        _db = db;
    }

    public override async Task<ScrapeResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var selectors = await _db.Selectors
                .Where(x => x.ScraperDefinitionId == _def.Id)
                .ToListAsync(cancellationToken);

            if (selectors.Count == 0)
                return Fail("No selectors defined.");

            var page = await PlaywrightContextFactory.CreatePageAsync();
            await page.GotoAsync(_def.BaseUrl);

            var results = new List<RawScrapedEvent>();
            var raw = new RawScrapedEvent();

            foreach (var sel in selectors)
            {
                List<string>? values = null;

                if (sel.SelectorType == Core.Domain.Enums.SelectorType.Css)
                {
                    var element = await page.QuerySelectorAllAsync(sel.Selector);

                    if (element is { Count: > 0 } all)
                    {
                        values = new();

                        foreach (var item in all)
                        {
                            var text = await item.InnerTextAsync();

                            values.Add(text);
                        }
                    }
                }

                raw.Fields[sel.FieldName] = values;
            }

            results.Add(raw);

            return Success(results);
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }
}
