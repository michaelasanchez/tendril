using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Engine.Abstractions;
using Tendril.Engine.Models;

namespace Tendril.Engine.Runtime;

// Internal helper to pass data back to the Executor
public record ScrapeYieldItem
{
    public RawScrapedEvent Data { get; init; } = new();
    public string? ChildUrl { get; init; }
    public Guid? ChildScraperId { get; init; }
}

public class DynamicScraper(IJsonLdProcessor jsonLd)
{
    private const int DefaultWait = 100;

    public async IAsyncEnumerable<ScrapeYieldItem> ExecuteAsync(
        IPage page,
        ScraperDefinition def)
    {
        // 1. PRE-SCRAPE PHASE
        // Run any interactions (like typing zip code) required BEFORE the list appears.
        var container = def.Selectors.Single(x => x.Type == SelectorType.Container);

        var preActions = def.Selectors
            .Where(x => x.Order < container.Order && x.Type != SelectorType.Container)
            .OrderBy(x => x.Order);

        foreach (var action in preActions)
        {
            await PerformActionAsync(page, null, action);
        }

        // 2. WAIT FOR CONTENT
        try
        {
            await page.WaitForSelectorAsync(container.Selector, new() { Timeout = 10000 });
        }
        catch (TimeoutException)
        {
            // Log warning or break if list never appears
            yield break;
        }

        // CHECK STRATEGY
        if (def.ExtractionStrategy is ExtractionStrategy.JsonLd)
        {
            // If we are here, we paid the "Playwright Tax" to render the page.
            // Now just grab the full HTML string and use the shared parser.
            var content = await page.ContentAsync();

            // Use the same helper used in StaticScraper (you'd inject this)
            var result = jsonLd.Extract(content, def.Selectors.FirstOrDefault()?.Selector ?? "Event");

            if (result != null)
            {
                yield return new ScrapeYieldItem { Data = result };
            }
            yield break; // Done.
        }

        // 3. PAGINATION LOOP
        bool hasMore = true;
        var processedSignatures = new HashSet<string>();

        do
        {
            // A. EXTRACT VISIBLE ITEMS
            var items = await page.QuerySelectorAllAsync(container.Selector);

            foreach (var item in items)
            {
                var result = new ScrapeYieldItem();

                // Run extraction selectors
                var itemSelectors = def.Selectors
                    .Where(x => x.Type != SelectorType.Container && !x.IsPaginationTrigger) // [ENTITY REQ] IsPaginationTrigger
                    .OrderBy(x => x.Order);

                foreach (var step in itemSelectors)
                {
                    // Check if this step triggers a child scraper (Deep Dive)
                    if (step.ChildScraperDefinitionId.HasValue) // [ENTITY REQ] ChildScraperDefinitionId
                    {
                        var linkEl = string.IsNullOrEmpty(step.Selector)
                            ? item
                            : await item.QuerySelectorAsync(step.Selector);

                        var url = await linkEl?.GetAttributeAsync("href");

                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            result = result with
                            {
                                ChildUrl = url,
                                ChildScraperId = step.ChildScraperDefinitionId
                            };
                        }
                    }
                    else
                    {
                        // Standard data extraction
                        await ExtractFieldAsync(page, item, step, result.Data);
                    }
                }

                // Dedup check (simple hash of fields)
                var signature = result.Data.GetSignature();
                if (processedSignatures.Add(signature))
                {
                    yield return result;
                }
            }

            // B. PAGINATION ACTION
            hasMore = await PerformPaginationAsync(page, def);

        } while (hasMore);
    }

    private async Task PerformActionAsync(IPage page, IElementHandle? scope, ScraperSelector action)
    {
        // Determine the element to act upon
        // If 'scope' is provided, look inside it. Otherwise, look at the full page.
        // If the selector is empty, act on the scope itself (e.g. clicking the list item).
        var target = string.IsNullOrWhiteSpace(action.Selector)
            ? scope
            : (scope != null
                ? await scope.QuerySelectorAsync(action.Selector)
                : await page.QuerySelectorAsync(action.Selector));

        if (target == null) return;

        switch (action.Type)
        {
            case SelectorType.Click:
                await target.ClickAsync();
                // Wait for potential navigation or DOM update
                await page.WaitForTimeoutAsync(action.Delay ?? DefaultWait);
                break;

            case SelectorType.Hover:
                await target.HoverAsync();
                await page.WaitForTimeoutAsync(action.Delay ?? DefaultWait);
                break;

            case SelectorType.Input: // [ENTITY REQ] New InteractionType
                if (!string.IsNullOrEmpty(action.InteractionValue))
                {
                    await target.FillAsync(action.InteractionValue);
                    await page.WaitForTimeoutAsync(action.Delay ?? DefaultWait);
                }
                break;

            case SelectorType.Scroll:
                // Use the scroll helper on this specific element
                await ScrollAsync(page, target, action.Delay);
                break;
        }
    }

    private static async Task ExtractFieldAsync(
        IPage page,
        IElementHandle item,
        ScraperSelector step,
        RawScrapedEvent rawEvent)
    {
        try
        {
            // Handle "Root" selectors (e.g., a modal that exists outside the <li>)
            // If step.Root is true, we ignore 'item' scope and query the Page directly.
            // (Note: You might need to pass IPage into this method if you support Root selectors deeply)
            IElementHandle? targetElement;

            if (step.Root) // [ENTITY REQ] Add IsRoot to Selector
            {
                // Look at the whole page, not just the list item
                targetElement = string.IsNullOrWhiteSpace(step.Selector)
                    ? null // Root requires a selector
                    : await page.QuerySelectorAsync(step.Selector);
            }
            else
            {
                // Look inside the list item
                targetElement = string.IsNullOrWhiteSpace(step.Selector)
                    ? item
                    : await item.QuerySelectorAsync(step.Selector);
            }

            // For simplicity in this snippet, assuming standard scoped selection:
            if (string.IsNullOrWhiteSpace(step.Selector))
            {
                targetElement = item;
            }
            else
            {
                targetElement = await item.QuerySelectorAsync(step.Selector);
            }

            if (targetElement == null) return;

            string? value = null;

            if (step.Type == SelectorType.CaptureLink)
            {
                try
                {
                    // 1. Setup the listener BEFORE clicking
                    // We ask the browser context: "Tell me when a new tab opens"
                    var popupTask = page.Context.WaitForPageAsync();

                    // 2. Click the button that triggers the popup
                    await targetElement.ClickAsync();

                    // 3. Await the new tab
                    var popupPage = await popupTask;

                    // 4. Wait for it to settle so the URL is accurate (handling redirects)
                    await popupPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                    // 5. Grab the URL
                    value = popupPage.Url;

                    // 6. Close the popup to clean up memory
                    await popupPage.CloseAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to capture popup URL: {ex.Message}");
                }
            }
            else
            {
                value = step.Type switch
                {
                    SelectorType.Text => await targetElement.InnerTextAsync(),
                    SelectorType.Attribute => await targetElement.GetAttributeAsync(step.AttributeName ?? ""),
                    _ => null
                };
            }

            if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(step.FieldName))
            {
                // Simple assignment. 
                // The EventMapper later handles type conversion (int, date, etc.)
                rawEvent.Fields[step.FieldName] = value.Trim();
            }
        }
        catch (Exception ex)
        {
            // Log locally or just continue. 
            // We don't want one missing field to crash the whole item.
            Console.WriteLine($"Error extracting field {step.FieldName}: {ex.Message}");
        }
    }

    private async Task<bool> PerformPaginationAsync(IPage page, ScraperDefinition def)
    {
        // [ENTITY REQ] PaginationType enum on Definition
        if (def.PaginationType == PaginationType.None) return false;

        if (def.PaginationType == PaginationType.InfiniteScroll)
        {
            return await ScrollAsync(page); // Uses your existing logic
        }

        if (def.PaginationType == PaginationType.NextButton)
        {
            var nextBtnDef = def.Selectors.FirstOrDefault(s => s.IsPaginationTrigger);
            if (nextBtnDef == null) return false;

            var nextBtn = await page.QuerySelectorAsync(nextBtnDef.Selector);
            if (nextBtn == null || await nextBtn.IsDisabledAsync()) return false;

            try
            {
                await nextBtn.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                return true;
            }
            catch { return false; }
        }

        return false;
    }
    private async Task<bool> ScrollAsync(IPage page, IElementHandle? element = null, int? delay = null)
    {
        long previousHeight = 0;
        long currentHeight = 0;
        int attempts = 0;
        int maxAttempts = 5; // Don't scroll forever if nothing happens

        // 1. Get initial height
        currentHeight = element != null
            ? await element.EvaluateAsync<long>("el => el.scrollHeight")
            : await page.EvaluateAsync<long>("() => document.body.scrollHeight");

        // 2. Scroll to bottom
        if (element != null)
        {
            await element.EvaluateAsync("el => el.scrollTo(0, el.scrollHeight)");
        }
        else
        {
            await page.EvaluateAsync("() => window.scrollTo(0, document.body.scrollHeight)");
        }

        // 3. Wait for load
        await page.WaitForTimeoutAsync(delay ?? 1000);

        // 4. Check new height
        previousHeight = currentHeight;
        currentHeight = element != null
            ? await element.EvaluateAsync<long>("el => el.scrollHeight")
            : await page.EvaluateAsync<long>("() => document.body.scrollHeight");

        // Returns true if content grew (meaning we successfully scrolled and found new stuff)
        return currentHeight > previousHeight;
    }
}
