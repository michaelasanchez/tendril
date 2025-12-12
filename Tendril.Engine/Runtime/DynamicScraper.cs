using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
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

            var innerSelectors = selectors.Where(x => x.Type != SelectorType.Container).ToList();

            if (innerSelectors.Count == 0)
                return Fail("No selectors defined.");

            var outerSelectors = selectors.Where(x => x.Type == SelectorType.Container).ToList();

            if (outerSelectors.Count != 1)
                return Fail("A single list selector is required.");

            var page = await PlaywrightContextFactory.CreatePageAsync();

            await page.GotoAsync(_def.BaseUrl);

            var containerSelector = selectors.Single(x => x.Type == SelectorType.Container);

            var pipelineSteps = selectors
                .Where(x => x.Type != SelectorType.Container)
                .OrderBy(x => x.Order)
                .ToList();

            var results = new List<RawScrapedEvent>();

            // 2. Initial Wait & Query
            await page.WaitForSelectorAsync(containerSelector.Selector);

            var items = await page.QuerySelectorAllAsync(containerSelector.Selector);

            foreach (var item in items)
            {
                var raw = new RawScrapedEvent();

                // 3. Execute the Pipeline
                foreach (var step in pipelineSteps)
                {
                    // 1. RESOLVE ELEMENT (The "Tasty" Switch)
                    var element = (string.IsNullOrWhiteSpace(step.Selector), step.Root) switch
                    {
                        (true, _) => item,                                     // Self (Container)
                        (false, true) => await page.QuerySelectorAsync(step.Selector), // Global (Root)
                        (false, false) => await item.QuerySelectorAsync(step.Selector)  // Scoped (Child)
                    };

                    // If element is missing, skip this step (or log it)
                    if (element is null) continue;

                    // 2. EXECUTE ACTION
                    if (step.Type == SelectorType.Click || step.Type == SelectorType.Hover)
                    {
                        // --- State Changes (Void) ---
                        if (step.Type == SelectorType.Hover)
                        {
                            await element.HoverAsync();
                        }
                        else // Click
                        {
                            await element.ClickAsync();
                        }

                        // Optional: Add a brief wait if your DB has a "WaitAfterMs" column
                        await page.WaitForTimeoutAsync(500);
                        //await page.WaitForTimeoutAsync(step.WaitAfterMs);
                    }
                    else
                    {
                        try
                        {
                            // --- Data Extraction (Returns String) ---
                            string? value = step.Type switch
                            {
                                SelectorType.Text => await element.InnerTextAsync(),
                                SelectorType.Href => await element.GetAttributeAsync("href"),
                                SelectorType.Src => await element.GetAttributeAsync("src"),
                                //SelectorType.Attribute => await element.GetAttributeAsync(step.AttributeName),
                                _ => null
                            };

                            if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(step.FieldName))
                            {
                                raw.Fields[step.FieldName] = value;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log and continue
                            Console.WriteLine($"Error processing step {step.Id}: {ex.Message}");
                            continue;
                        }
                    }
                }

                // Only add if we actually found data (optional validation)
                if (raw.Fields.Count > 0)
                {
                    results.Add(raw);
                }
            }

            return Success(results);
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }
}
