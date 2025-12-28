using Microsoft.Playwright;
using System.Runtime.CompilerServices;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;
using Tendril.Engine.Abstractions;
using Tendril.Engine.Models;
using Tendril.Engine.Playwright;

namespace Tendril.Engine.Runtime;

public class ScrapeExecutor(DynamicScraper scraper, IScraperRepository repo) : IScrapeExecutor
{
    public async IAsyncEnumerable<RawScrapedEvent> RunScraperAsync(
    ScraperDefinition def,
    [EnumeratorCancellation] CancellationToken ct)
    {
        // 1. Setup Context
        await using var context = await PlaywrightContextFactory.CreateContextAsync();

        var mainPage = await context.NewPageAsync();

        await mainPage.GotoAsync(def.BaseUrl);

        //var metadata = await ExtractMetaTagsAsync(mainPage);

        // 2. Stream & Merge
        await foreach (var item in scraper.ExecuteAsync(mainPage, def).WithCancellation(ct))
        {
            if (item.ChildUrl == null)
            {
                yield return item.Data;
            }
            else
            {
                var childEvent = await RunChildScrapeAsync(context, item, ct);

                if (childEvent != null) yield return childEvent;
            }
        }
    }

    private async Task<RawScrapedEvent?> RunChildScrapeAsync(
        IBrowserContext context,
        ScrapeYieldItem parentItem,
        CancellationToken ct)
    {
        // 1. Fetch Child Definition
        var childDef = await repo.GetByIdAsync(parentItem.ChildScraperId!.Value, ct);

        if (childDef == null) return null;

        // 2. Open New Tab
        var childPage = await context.NewPageAsync();

        await childPage.GotoAsync(parentItem.ChildUrl);

        RawScrapedEvent? mergedData = null;

        // 3. Execute Child Scraper
        // We assume the child scraper yields exactly one "Full Details" item,
        // or we take the first one if it yields multiple.
        await foreach (var childItem in scraper.ExecuteAsync(childPage, childDef).WithCancellation(ct))
        {
            // Merge Logic: Child overrides Parent
            mergedData = MergeEvents(parentItem.Data, childItem.Data);

            break; // Stop after first item (assuming 1:1 relationship)
        }

        await childPage.CloseAsync();

        return mergedData;
    }
    private async Task<Dictionary<string, string?>> ExtractMetaTagsAsync(IPage page)
    {
        // Run a quick JS function to grab all og: tags at once
        return await page.EvaluateAsync<Dictionary<string, string?>>(@"() => {
        const result = {};
        
        // Get Open Graph tags
        document.querySelectorAll('meta[property^=""og:""]').forEach(tag => {
            const prop = tag.getAttribute('property');
            const content = tag.getAttribute('content');
            if (prop && content) result[prop] = content;
        });

        // Get standard description if og:description is missing
        const desc = document.querySelector('meta[name=""description""]');
        if (desc && desc.content) result['description'] = desc.content;

        return result;
    }");
    }

    private RawScrapedEvent MergeEvents(RawScrapedEvent parent, RawScrapedEvent child)
    {
        // Simple dictionary merge
        var merged = new RawScrapedEvent();

        foreach (var kvp in parent.Fields) merged.Fields[kvp.Key] = kvp.Value;
        foreach (var kvp in child.Fields) merged.Fields[kvp.Key] = kvp.Value; // Child wins collision

        return merged;
    }
}