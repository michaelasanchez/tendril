using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Data;
using Tendril.Engine.Models;
using Tendril.Engine.Playwright;

namespace Tendril.Engine.Runtime;

public class DynamicScraper : BaseScraper
{
    private const int DefaultClickDelay = 500;
    private const int DefaultScrollDelay = 1000;

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
            await ScrollAsync(page);

            await page.WaitForSelectorAsync(containerSelector.Selector);

            var items = await page.QuerySelectorAllAsync(containerSelector.Selector);

            foreach (var item in items)
            {
                var raw = new RawScrapedEvent();

                // 3. Execute the Pipeline
                foreach (var step in pipelineSteps)
                {
                    var selectorIsEmpty = string.IsNullOrWhiteSpace(step.Selector);
                    var selectorIsRoot = step.Root;

                    var element = (selectorIsEmpty, selectorIsRoot) switch
                    {
                        (true, _) => item,
                        (false, true) => await page.QuerySelectorAsync(step.Selector),
                        (false, false) => await item.QuerySelectorAsync(step.Selector)
                    };

                    if (element is null) continue;

                    if (step.Type == SelectorType.Click || step.Type == SelectorType.Hover)
                    {
                        if (step.Type == SelectorType.Hover)
                        {
                            await element.HoverAsync();
                        }
                        else
                        {
                            await element.ClickAsync();
                        }

                        await page.WaitForTimeoutAsync(step.Delay ?? DefaultClickDelay);
                    }
                    else if (step.Type == SelectorType.Scroll)
                    {
                        await ScrollAsync(page, element, step.Delay);
                    }
                    else
                    {
                        try
                        {
                            string? value = step.Type switch
                            {
                                SelectorType.Text => await element.InnerTextAsync(),
                                // TODO: deprecate in favor of Attribute?
                                SelectorType.Href => await element.GetAttributeAsync("href"),
                                SelectorType.Src => await element.GetAttributeAsync("src"),
                                //
                                SelectorType.Attribute => await element.GetAttributeAsync(step.AttributeName),
                                _ => null
                            };

                            if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(step.FieldName))
                            {
                                raw.Fields[step.FieldName] = value;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing step {step.Id}: {ex.Message}");
                            continue;
                        }
                    }
                }

                if (raw.Fields.Count > 0)
                {
                    results.Add(raw);
                }
            }

            // TODO: would be nice to add additional information on this, such as:
            //  - scroll count
            return Success(results);
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }
    private async Task<int> ScrollAsync(IPage page, IElementHandle? element = null, int? delay = null)
    {
        var maxScrolls = 50;
        var scrollCount = 0;
        long previousHeight = 0;

        while (scrollCount < maxScrolls)
        {
            // 1. Get current scroll height
            // If element is null, we check the document body height
            long currentHeight = element != null
                ? await element.EvaluateAsync<long>("el => el.scrollHeight")
                : await page.EvaluateAsync<long>("() => document.body.scrollHeight");

            // 2. If height hasn't changed since last scroll, we've hit the bottom
            // (Note: checking > 0 ensures we don't break on the very first pass if prev is 0)
            if (currentHeight == previousHeight)
            {
                break;
            }

            // 3. Scroll to the bottom
            if (element != null)
            {
                await element.EvaluateAsync("el => el.scrollTo(0, el.scrollHeight)");
            }
            else
            {
                // Scroll the main window
                await page.EvaluateAsync("() => window.scrollTo(0, document.body.scrollHeight)");
            }

            // 4. Wait for content to load
            await page.WaitForTimeoutAsync(delay ?? DefaultScrollDelay);

            previousHeight = currentHeight;
            scrollCount++;
        }

        return scrollCount;
    }
}
