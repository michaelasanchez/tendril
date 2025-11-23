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

    public override async Task<ScrapeResult> ExecuteAsync(bool selectorsOnly, CancellationToken cancellationToken = default)
    {
        try
        {
            var selectors = await _db.Selectors
                .Where(x => x.ScraperDefinitionId == _def.Id)
                .ToListAsync(cancellationToken);

            var innerSelectors = selectors.Where(x => !x.Outer).ToList();

            if (innerSelectors.Count == 0)
                return Fail("No selectors defined.");

            var outerSelectors = selectors.Where(x => x.Outer).ToList();

            if (outerSelectors.Count != 1)
                return Fail("A single list selector is required.");

            var page = await PlaywrightContextFactory.CreatePageAsync();
            await page.GotoAsync(_def.BaseUrl);

            var results = new List<RawScrapedEvent>();

            var items = await page.QuerySelectorAllAsync(outerSelectors.Single().Selector);

            foreach (var item in items)
            {
                var raw = new RawScrapedEvent();

                foreach (var sel in innerSelectors)
                {
                    string? value = null;

                    if (sel.SelectorType == Core.Domain.Enums.SelectorType.Css)
                    {
                        var element = await item.QuerySelectorAsync(sel.Selector);
                        
                        var tag = await element.EvaluateAsync<string>("e => e.tagName.toLowerCase()");

                        if (tag == "a")
                        {
                            var href = await element.EvaluateAsync<string>("e => e.getAttribute('href')");

                            raw.Fields["Href"] = href;
                        }

                        value = element is not null ? await element.InnerTextAsync() : null;
                    }

                    raw.Fields[sel.FieldName] = value;
                }

                results.Add(raw);
            }

            return Success(results);
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }
}
