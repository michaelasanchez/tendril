using Microsoft.Playwright;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Engine.Abstractions;
using Tendril.Engine.Extensions;
using Tendril.Engine.Models;

namespace Tendril.Engine.Scrapers;

public class DynamicScraper(IJsonLdProcessor jsonLd)
{
    public async IAsyncEnumerable<ScrapeYieldItem> ExecuteAsync(
        IPage page,
        ScraperDefinition definition,
        ScrapeContext context)
    {
        // 1. NAVIGATION
        try
        {
            await page.GotoAsync(definition.BaseUrl, new PageGotoOptions { Timeout = 30000 });
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to navigate to {definition.BaseUrl}: {ex.Message}", ex);
        }

        // 2. CHECK STRATEGY
        if (definition.ExtractionStrategy is ExtractionStrategy.JsonLd)
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
                var result = jsonLd.Extract(content, definition.Actions.FirstOrDefault()?.Selector ?? "Event");

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

        // 3. PRE-SCRAPE PHASE
        // Run any interactions (like typing zip code) required BEFORE the list appears.
        var container = definition.Actions.Single(x => x.Type == ActionType.Container);

        var preSelectors = definition.Actions
            .Where(x => x.Order < container.Order && x.Type != ActionType.Container)
            .OrderBy(x => x.Order);

        var preResult = context.ParentItem ?? new ScrapeYieldItem();

        foreach (var step in preSelectors)
        {
            await ProcessSelector(page, null, step, preResult.Data);
        }

        // 4. WAIT FOR CONTENT
        try
        {
            await page.WaitForSelectorAsync(container.Selector, new() { Timeout = 10000 });
        }
        catch (TimeoutException)
        {
            // Log warning or break if list never appears
            yield break;
        }

        // 5. PAGINATION LOOP
        bool hasMore = true;
        var processedSignatures = new HashSet<string>();

        do
        {
            // A. EXTRACT VISIBLE ITEMS
            var items = await page.QuerySelectorAllAsync(container.Selector);

            foreach (var item in items)
            {
                var result = new ScrapeYieldItem();

                var partial = false;

                var fieldSelectors = definition.Actions
                    .Where(x => x.Type != ActionType.Container && !x.IsPaginationTrigger)
                    .OrderBy(x => x.Order);

                foreach (var step in fieldSelectors)
                {
                    // Check if this step triggers a child scraper (Deep Dive)
                    if (step.Type == ActionType.FollowLink && step.ChildScraperDefinitionId.HasValue)
                    {
                        var linkEl = string.IsNullOrEmpty(step.Selector)
                            ? item
                            : await item.QuerySelectorAsync(step.Selector);

                        if (linkEl is not null)
                        {
                            var url = await linkEl.GetAttributeAsync("href");

                            result.Data.Fields[step.OutputField] = url;

                            if (!string.IsNullOrWhiteSpace(url) && (!step.IgnoreDuplicateUrls || !context.HasVisited(url)))
                            {
                                context.MarkVisited(url); // Claim it now so subsequent items skip it

                                result = result with
                                {
                                    ChildUrl = url,
                                    ChildScraperId = step.ChildScraperDefinitionId
                                };
                            }
                            else
                            {
                                partial = true;
                                break; // Stop processing this specific list item
                            }
                        }
                    }
                    else if (step.Type == ActionType.CallApi && step.ChildScraperDefinitionId.HasValue)
                    {
                        context.ParentIgnoreDuplicateUrls = step.IgnoreDuplicateUrls;

                        result = result with
                        {
                            ChildScraperId = step.ChildScraperDefinitionId
                        };
                    }
                    else
                    {
                        // Standard data extraction
                        await ProcessSelector(page, item, step, result.Data);
                    }
                }

                // Dedup check (simple hash of fields)
                var signature = result.Data.GetSignature();

                if (processedSignatures.Add(signature) && !partial)
                {
                    yield return preResult.Merge(result);
                }
            }

            // B. PAGINATION ACTION
            if (items.Count == 0)
            {
                hasMore = false;
            }
            else
            {
                hasMore = await PerformPagination(page, definition);
            }

        } while (hasMore);
    }

    private static async Task ProcessSelector(
        IPage page,
        IElementHandle? item,
        ScraperAction step,
        RawScrapedData? scrapedData)
    {
        // 1. Exclusions
        if (step.Type is ActionType.Container or ActionType.FollowLink) return;

        try
        {
            // 2. Determine Target
            IElementHandle? targetElement;

            // Logic: If explicitly Root OR if we have no item scope (Pre-Scrape), query the Page.
            if (step.Root || item == null)
            {
                targetElement = await page.QuerySelectorAsync(step.Selector);
            }
            else
            {
                // Scoped: Look inside the provided list item
                targetElement = string.IsNullOrWhiteSpace(step.Selector)
                    ? item
                    : await item.QuerySelectorAsync(step.Selector);
            }

            if (targetElement == null) return;

            // 3. Handle Interactions (Click, Input, Scroll, Hover)
            switch (step.Type)
            {
                case ActionType.Click:
                {
                    await targetElement.ClickAsync();
                    await PerformWait(page, step.Delay);
                    return;
                }

                case ActionType.Hover:
                {
                    await targetElement.HoverAsync();
                    await PerformWait(page, step.Delay);
                    return;
                }

                case ActionType.Input:
                {
                    if (!string.IsNullOrEmpty(step.InteractionValue))
                    {
                        await targetElement.FillAsync(step.InteractionValue);
                        await PerformWait(page, step.Delay);
                    }
                    return;
                }

                case ActionType.Scroll:
                {
                    await PerformScroll(page, targetElement, step.Delay);
                    return;
                }
            }

            // 4. Handle Data Extraction (Text, Attribute, CaptureLink)
            // If we don't have an event object to write to, we can stop here.
            if (scrapedData == null) return;

            string? value = null;

            if (step.Type == ActionType.CaptureLink)
            {
                // TODO: Looks like this was only ever implemented in StaticScraper
                // [Insert your existing CaptureLink logic here]
                // value = popupPage.Url;
            }
            else if (step.Type == ActionType.Text)
            {
                value = await targetElement.InnerTextAsync();
            }
            else if (step.Type == ActionType.Attribute)
            {
                // FIX: If we are grabbing an 'href' or 'src', ask the browser for the computed property
                if (step.AttributeName is "href" or "src")
                {
                    // "el.href" returns the full absolute URL (http://...)
                    // "el.getAttribute('href')" returns the relative string (/ticket/...)
                    value = await targetElement.EvaluateAsync<string>($"el => el.{step.AttributeName}");
                }
                else
                {
                    value = await targetElement.GetAttributeAsync(step.AttributeName ?? "");
                }
            }
            else if (step.Type == ActionType.ConstantValue)
            {
                value = step.ConstantValue;
            }

            // 5. Assign to Event
            if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(step.OutputField))
            {
                scrapedData.Fields[step.OutputField] = value.Trim();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing step {step.Selector}: {ex.Message}");
        }
    }

    private static async Task<bool> PerformPagination(IPage page, ScraperDefinition def)
    {
        if (def.PaginationType == PaginationType.None) return false;

        if (def.PaginationType == PaginationType.InfiniteScroll)
        {
            return await PerformScroll(page);
        }

        if (def.PaginationType == PaginationType.NextButton)
        {
            var nextBtnDef = def.Actions.FirstOrDefault(s => s.IsPaginationTrigger);
            if (nextBtnDef == null) return false;

            var nextBtn = await page.QuerySelectorAsync(nextBtnDef.Selector);
            if (nextBtn == null || await nextBtn.IsDisabledAsync()) return false;

            try
            {
                await nextBtn.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                await PerformWait(page, nextBtnDef.Delay);

                return true;
            }
            catch { return false; }
        }

        return false;
    }

    private static async Task<bool> PerformScroll(IPage page, IElementHandle? element = null, int? delay = null)
    {
        // 1. Capture the height BEFORE we scroll
        long initialHeight = element != null
            ? await element.EvaluateAsync<long>("el => el.scrollHeight")
            : await page.EvaluateAsync<long>("() => document.body.scrollHeight");

        // 2. Trigger the scroll action
        if (element != null)
        {
            await element.EvaluateAsync("el => el.scrollTo(0, el.scrollHeight)");
        }
        else
        {
            await page.EvaluateAsync("() => window.scrollTo(0, document.body.scrollHeight)");
        }

        // 3. The "Verification Loop"
        // We check up to 3 times to see if the height increases.
        int attempts = 0;
        while (attempts < 3)
        {
            // Use your passed-in delay, or default to 1.5s for "hot" runs
            await page.WaitForTimeoutAsync(delay ?? 1500);

            long currentHeight = element != null
                ? await element.EvaluateAsync<long>("el => el.scrollHeight")
                : await page.EvaluateAsync<long>("() => document.body.scrollHeight");

            if (currentHeight > initialHeight)
            {
                return true; // The page grew! More items should be visible now.
            }

            // If it didn't grow, try one more nudge. 
            // Sometimes the first scroll doesn't trigger the "onScroll" listener if it was too fast.
            if (element != null)
                await element.EvaluateAsync("el => el.scrollTo(0, el.scrollHeight)");
            else
                await page.EvaluateAsync("() => window.scrollTo(0, document.body.scrollHeight)");

            attempts++;
        }

        return false; // We gave it 3 tries and ~4.5 seconds; nothing happened.
    }

    private static async Task PerformWait(IPage page, int? delay)
    {
        if (delay.HasValue && delay.Value > 0)
        {
            await page.WaitForTimeoutAsync(delay.Value);
        }
    }

}
