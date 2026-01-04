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
        // CHECK STRATEGY
        if (def.ExtractionStrategy is ExtractionStrategy.JsonLd)
        {
            var maxRetries = 3;
            var retry = 0;

            while (retry < maxRetries)
            {

                // If we are here, we paid the "Playwright Tax" to render the page.
                // Now just grab the full HTML string and use the shared parser.
                var content = await page.ContentAsync();

                try
                {
                    // We wait up to 5 seconds for the script tag to appear in the DOM
                    await page.WaitForSelectorAsync("script[type='application/ld+json']", new PageWaitForSelectorOptions
                    {
                        State = WaitForSelectorState.Attached,
                        Timeout = 5000
                    });
                }
                catch (TimeoutException)
                {
                    // If it doesn't show up in 5s, we proceed. 
                    // It might just not be there, or the page is slow.
                    // The processor will return null gracefully below.
                }

                // Use the same helper used in StaticScraper (you'd inject this)
                var result = jsonLd.Extract(content, def.Selectors.FirstOrDefault()?.Selector ?? "ComedyEvent");

                if (result != null)
                {
                    yield return new ScrapeYieldItem { Data = result };
                }

                retry++;
            }

            // TODO: here we should log that we waited/retried three times and got nothing,
            //  probably even log the content
            yield break; // Done.
        }

        // 1. PRE-SCRAPE PHASE
        // Run any interactions (like typing zip code) required BEFORE the list appears.
        var container = def.Selectors.Single(x => x.Type == SelectorType.Container);

        var preActions = def.Selectors
            .Where(x => x.Order < container.Order && x.Type != SelectorType.Container)
            .OrderBy(x => x.Order);

        foreach (var action in preActions)
        {
            await PerformPreActionAsync(page, null, action);
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
                    .Where(x => x.Type != SelectorType.Container && !x.IsPaginationTrigger)
                    .OrderBy(x => x.Order);

                foreach (var step in itemSelectors)
                {
                    // Check if this step triggers a child scraper (Deep Dive)
                    if (step.ChildScraperDefinitionId.HasValue)
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
                        await ProcessStepAsync(page, item, step, result.Data);
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

    private static async Task PerformPreActionAsync(IPage page, IElementHandle? scope, ScraperSelector action)
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

            case SelectorType.Input:
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

    private static async Task ProcessStepAsync(
        IPage page,
        IElementHandle item,
        ScraperSelector step,
        RawScrapedEvent rawEvent)
    {
        // 1. Exclusions: These types should be handled by the main loop, not here.
        if (step.Type is SelectorType.Container or SelectorType.FollowLink) return;

        try
        {
            // 2. Determine Target (Root vs Scoped)
            IElementHandle? targetElement;

            if (step.Root)
            {
                targetElement = string.IsNullOrWhiteSpace(step.Selector)
                    ? null
                    : await page.QuerySelectorAsync(step.Selector);
            }
            else
            {
                targetElement = string.IsNullOrWhiteSpace(step.Selector)
                    ? item
                    : await item.QuerySelectorAsync(step.Selector);
            }

            if (targetElement == null) return;

            // 3. Handle Interactions (Void actions)
            switch (step.Type)
            {
                case SelectorType.Click:
                    await targetElement.ClickAsync();
                    await Wait(page, step.Delay);
                    return;

                case SelectorType.Hover:
                    await targetElement.HoverAsync();
                    await Wait(page, step.Delay);
                    return;

                case SelectorType.Input:
                    if (!string.IsNullOrEmpty(step.InteractionValue))
                    {
                        await targetElement.FillAsync(step.InteractionValue);
                        await Wait(page, step.Delay);
                    }
                    return;

                case SelectorType.Scroll:
                    // Reusing your ScrollAsync helper logic here
                    // Note: You might need to make ScrollAsync static or move it to a helper class
                    await ScrollAsync(page, targetElement, step.Delay);
                    return;
            }

            // 4. Handle Data Extraction (Returns a value)
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
            else if (step.Type == SelectorType.Text)
            {
                value = await targetElement.InnerTextAsync();
            }
            else if (step.Type == SelectorType.Attribute)
            {
                value = await targetElement.GetAttributeAsync(step.AttributeName ?? "");
            }

            // 5. Assign to Event
            if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(step.FieldName))
            {
                rawEvent.Fields[step.FieldName] = value.Trim();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing step {step.Selector}: {ex.Message}");
        }
    }

    // Small helper to keep the switch clean
    private static async Task Wait(IPage page, int? delay)
    {
        if (delay.HasValue && delay.Value > 0)
        {
            await page.WaitForTimeoutAsync(delay.Value);
        }
    }

    private static async Task<bool> PerformPaginationAsync(IPage page, ScraperDefinition def)
    {
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
    private static async Task<bool> ScrollAsync(IPage page, IElementHandle? element = null, int? delay = null)
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
