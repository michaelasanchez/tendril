using Microsoft.Playwright;
using System.Runtime.CompilerServices;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Engine.Extensions;
using Tendril.Engine.Interfaces;
using Tendril.Engine.Models;

namespace Tendril.Engine.Scrapers;

public class DynamicScraper(IJsonLdProcessor jsonLd)
{
    private static readonly string ProcessedClass = "tendril-processed";

    public async IAsyncEnumerable<ScrapeYieldItem> ExecuteAsync(
        IPage page,
        ScraperDefinition definition,
        ScrapeContext context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // 1. NAVIGATION
        try
        {
            if (page.Url != definition.BaseUrl)
            {
                await page.GotoAsync(definition.BaseUrl, new PageGotoOptions { Timeout = 30000 });
            }
        }
        catch (Exception ex)
        {
            context.AddLog(ScraperAttemptLogLevel.Error,
                $"Failed to navigate to scraper URL '{definition.BaseUrl}': {ex.Message}");

            throw new Exception($"Failed to navigate to {definition.BaseUrl}: {ex.Message}", ex);
        }

        // 2. CHECK STRATEGY
        if (definition.ExtractionStrategy is ExtractionStrategy.JsonLd)
        {
            context.AddLog(ScraperAttemptLogLevel.Information, "Using JSON-LD extraction strategy.");

            await foreach (var item in RunJsonLdExtraction(page, definition, context, ct))
            {
                yield return item;
            }

            yield break;
        }

        // 3. PRE-SCRAPE PHASE
        // Run any interactions (like typing zip code) required BEFORE the list appears.
        var containerAction = definition.Actions.Single(x => x.Type == ActionType.Container);

        var preSelectors = definition.Actions
            .Where(x => x.Order < containerAction.Order && x.Type != ActionType.Container)
            .OrderBy(x => x.Order);

        var preResult = (context.ParentItem ?? new ScrapeYieldItem()) with
        {
            ChildScraperId = null,
            ChildUrl = null,
        };

        foreach (var step in preSelectors)
        {
            await PerformAction(page, null, step, preResult.Data, context);
        }

        // 4. WAIT FOR CONTENT
        try
        {
            await page.WaitForSelectorAsync(containerAction.Selector, new() { Timeout = containerAction.Delay ?? 10000 });
        }
        catch (TimeoutException)
        {
            context.AddLog(ScraperAttemptLogLevel.Warning,
                $"Timed out waiting for container selector '{containerAction.Selector}' at '{page.Url}'.");
            yield break;
        }

        // 5. PAGINATION LOOP
        bool hasMore = true;
        var processedSignatures = new HashSet<string>();

        do
        {
            ct.ThrowIfCancellationRequested();

            var pageUrl = page.Url;
            var items = await page.QuerySelectorAllAsync($"{containerAction.Selector}:not(.{ProcessedClass})");

            context.AddLog(ScraperAttemptLogLevel.Information, $"Selected {items.Count} items(s) on page '{pageUrl}';");

            foreach (var item in items)
            {
                ct.ThrowIfCancellationRequested();

                var result = new ScrapeYieldItem();

                var partial = false;

                var itemActions = definition.Actions
                    .Where(x => x.Type != ActionType.Container && !x.IsPaginationTrigger)
                    .OrderBy(x => x.Order);

                foreach (var action in itemActions)
                {
                    if (action.Type is ActionType.Synchronize)
                    {
                        await PerformSynchronization(page, item, action, result.Data, context);
                    }
                    else if (action.Type == ActionType.FollowLink && action.ChildScraperDefinitionId.HasValue)
                    {
                        var linkEl = string.IsNullOrEmpty(action.Selector)
                            ? item
                            : await item.QuerySelectorAsync(action.Selector);

                        if (linkEl is not null)
                        {
                            var url = await linkEl.GetAttributeAsync("href");

                            result.Data.Fields[action.OutputField] = url;

                            if (!string.IsNullOrWhiteSpace(url) && (action.AllowDuplicateUrls is true || !context.HasVisited(url)))
                            {
                                context.MarkVisited(url); // Claim it now so subsequent items skip it

                                result = result with
                                {
                                    ChildScraperId = action.ChildScraperDefinitionId,
                                    ChildUrl = url,
                                    AllowEmptyResult = action.AllowDuplicateUrls
                                };
                            }
                            else
                            {
                                partial = true;
                                break; // Stop processing this specific list item
                            }
                        }
                    }
                    else if (action.Type == ActionType.CallApi && action.ChildScraperDefinitionId.HasValue)
                    {
                        result = result with
                        {
                            ChildScraperId = action.ChildScraperDefinitionId,
                            AllowDuplicateUrls = action.AllowDuplicateUrls,
                            AllowEmptyResult = action.AllowEmptyResult
                        };
                    }
                    else
                    {
                        await PerformAction(page, item, action, result.Data, context);
                    }
                }

                await item.EvaluateAsync($"el => el.classList.add('{ProcessedClass}')");

                // Dedup check (simple hash of fields)
                var signature = result.Data.GetSignature();

                if (processedSignatures.Add(signature) && !partial)
                {
                    yield return preResult.Merge(result);
                }
            }

            // B. PAGINATION ACTION
            if (!items.Any())
            {
                hasMore = false;
                context.AddLog(ScraperAttemptLogLevel.Information,
                    $"no more items found.");

                if (context.ParentItem?.AllowEmptyResult == true)
                {
                    yield return preResult; // Yield what we have, even if it's empty
                }
            }
            else
            {
                hasMore = await PerformPagination(page, definition, context, pageUrl);

                if (!hasMore)
                {
                    context.AddLog(ScraperAttemptLogLevel.Warning,
                        $"Pagination attempt did not succeed. ({definition.PaginationType})");
                }
            }

        } while (hasMore);
    }

    private static async Task PerformAction(
        IPage page,
        IElementHandle? item,
        ScraperAction action,
        RawScrapedData? scrapedData,
        ScrapeContext context
    )
    {
        // 1. Exclusions
        if (action.Type is ActionType.Container or ActionType.FollowLink) return;

        try
        {
            // 2. Determine Target
            IElementHandle? targetElement;

            // Logic: If explicitly Root OR if we have no item scope (Pre-Scrape), query the Page.
            if (action.Root || item == null)
            {
                targetElement = await page.QuerySelectorAsync(action.Selector);
            }
            else
            {
                // Scoped: Look inside the provided list item
                targetElement = string.IsNullOrWhiteSpace(action.Selector)
                    ? item
                    : await item.QuerySelectorAsync(action.Selector);
            }

            if (targetElement == null)
            {
                return;
            }

            // 3. Handle Interactions (Click, Input, Scroll, Hover)
            switch (action.Type)
            {
                case ActionType.Click:
                {
                    await targetElement.ClickAsync();
                    await PerformWait(page, action.Delay);

                    context.AddLog(ScraperAttemptLogLevel.Information, $"Click on {action.Selector} succeeded.");
                    return;
                }

                case ActionType.Hover:
                {
                    await targetElement.HoverAsync();
                    await PerformWait(page, action.Delay);
                    return;
                }

                case ActionType.Input:
                {
                    if (!string.IsNullOrEmpty(action.InteractionValue))
                    {
                        await targetElement.FillAsync(action.InteractionValue);
                        await PerformWait(page, action.Delay);
                    }
                    return;
                }

                case ActionType.Scroll:
                {
                    await PerformScroll(page, targetElement);
                    return;
                }
            }

            // 4. Handle Data Extraction (Text, Attribute, CaptureLink)
            // If we don't have an event object to write to, we can stop here.
            if (scrapedData == null) return;

            string? value = null;

            if (action.Type == ActionType.CaptureLink)
            {
                // TODO: Looks like this was only ever implemented in StaticScraper
                // [Insert your existing CaptureLink logic here]
                // value = popupPage.Url;
            }
            else if (action.Type == ActionType.Text)
            {
                value = await targetElement.InnerTextAsync();
            }
            else if (action.Type == ActionType.Attribute)
            {
                // FIX: If we are grabbing an 'href' or 'src', ask the browser for the computed property
                if (action.AttributeName is "href" or "src")
                {
                    // "el.href" returns the full absolute URL (http://...)
                    // "el.getAttribute('href')" returns the relative string (/ticket/...)
                    value = await targetElement.EvaluateAsync<string>($"el => el.{action.AttributeName}");
                }
                else
                {
                    value = await targetElement.GetAttributeAsync(action.AttributeName ?? "");
                }
            }
            else if (action.Type == ActionType.ConstantValue)
            {
                value = action.ConstantValue;
            }

            // 5. Assign to Event
            if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(action.OutputField))
            {
                scrapedData.Fields[action.OutputField] = value.Trim();
            }
        }
        catch (OperationCanceledException)
        {
            context.AddLog(ScraperAttemptLogLevel.Warning,
                $"Processing selector '{action.Selector}' at '{page.Url}' was canceled.");
            throw;
        }
        catch (TimeoutException ex)
        {
            context.AddLog(ScraperAttemptLogLevel.Warning,
                $"Timed out processing selector '{action.Selector}' at '{page.Url}': {ex.Message}");
        }
        catch (Exception ex)
        {
            context.AddLog(ScraperAttemptLogLevel.Warning,
                $"Failed to process selector '{action.Selector}' at '{page.Url}': {ex.Message}");
        }
    }

    private async IAsyncEnumerable<ScrapeYieldItem> RunJsonLdExtraction(
        IPage page,
        ScraperDefinition definition,
        ScrapeContext context,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var allowedActions = new HashSet<ActionType>
        {
            ActionType.Synchronize,
            ActionType.Click,
            ActionType.Hover,
            ActionType.Input,
            ActionType.Scroll
        };

        var actions = definition.Actions
            .Where(x => allowedActions.Contains(x.Type) && !x.IsPaginationTrigger)
            .OrderBy(x => x.Order);

        foreach (var action in actions)
        {
            ct.ThrowIfCancellationRequested();

            if (action.Type is ActionType.Synchronize)
            {
                await PerformSynchronization(page, null, action, null, context);
            }
            else
            {
                await PerformAction(page, null, action, null, context);
            }
        }

        var targetType = /*definition.Actions.FirstOrDefault()?.Selector ?? */"Event";
        var processedSignatures = new HashSet<string>();

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                await page.WaitForSelectorAsync("script[type='application/ld+json']", new PageWaitForSelectorOptions
                {
                    State = WaitForSelectorState.Attached,
                    Timeout = 5000
                });
            }
            catch (TimeoutException)
            {
                context.AddLog(ScraperAttemptLogLevel.Warning,
                    $"Timed out waiting for JSON-LD content at '{page.Url}'; extraction will continue.");
            }

            var content = await page.ContentAsync();

            foreach (var data in jsonLd.ExtractAll(content, targetType))
            {
                ct.ThrowIfCancellationRequested();

                if (processedSignatures.Add(data.GetSignature()))
                    yield return new ScrapeYieldItem { Data = data };
            }

            if (definition.PaginationType == PaginationType.None)
                yield break;

            var pageUrl = page.Url;
            var hasMore = await PerformPagination(page, definition, context, pageUrl);

            if (!hasMore)
            {
                context.AddLog(ScraperAttemptLogLevel.Warning,
                    $"Pagination attempt did not succeed. ({definition.PaginationType})");
                yield break;
            }
        }
    }

    private static async Task PerformSynchronization(
        IPage page,
        IElementHandle? item,
        ScraperAction action,
        RawScrapedData? scrapedData,
        ScrapeContext context
    )
    {
        if (action.Type != ActionType.Synchronize) return;

        try
        {
            switch (action.Synchronization)
            {
                case SynchronizationCondition.Delay:
                    await PerformWait(page, action.Delay);
                    break;

                case SynchronizationCondition.SelectorAttached:
                case SynchronizationCondition.SelectorVisible:
                case SynchronizationCondition.SelectorHidden:
                {
                    if (string.IsNullOrWhiteSpace(action.Selector))
                    {
                        context.AddLog(ScraperAttemptLogLevel.Warning,
                            $"Synchronization condition '{action.Synchronization}' requires a selector.");
                        return;
                    }

                    var state = action.Synchronization switch
                    {
                        SynchronizationCondition.SelectorAttached => WaitForSelectorState.Attached,
                        SynchronizationCondition.SelectorVisible => WaitForSelectorState.Visible,
                        _ => WaitForSelectorState.Hidden
                    };
                    var timeout = action.Delay ?? 10000;

                    if (action.Root || item is null)
                    {
                        await page.WaitForSelectorAsync(action.Selector, new PageWaitForSelectorOptions
                        {
                            State = state,
                            Timeout = timeout
                        });
                    }
                    else
                    {
                        await item.WaitForSelectorAsync(action.Selector, new ElementHandleWaitForSelectorOptions
                        {
                            State = state,
                            Timeout = timeout
                        });
                    }

                    break;
                }

                case SynchronizationCondition.DomContentLoaded:
                    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded,
                        new PageWaitForLoadStateOptions { Timeout = action.Delay ?? 10000 });
                    break;

                case SynchronizationCondition.Load:
                    await page.WaitForLoadStateAsync(LoadState.Load,
                        new PageWaitForLoadStateOptions { Timeout = action.Delay ?? 10000 });
                    break;

                case SynchronizationCondition.NetworkIdle:
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle,
                        new PageWaitForLoadStateOptions { Timeout = action.Delay ?? 10000 });
                    break;

                case SynchronizationCondition.JsonLdPresent:
                    await page.WaitForSelectorAsync("script[type='application/ld+json']", new PageWaitForSelectorOptions
                    {
                        State = WaitForSelectorState.Attached,
                        Timeout = action.Delay ?? 10000
                    });
                    break;

                default:
                    context.AddLog(ScraperAttemptLogLevel.Warning,
                        $"Unsupported synchronization condition '{action.Synchronization}'.");
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            context.AddLog(ScraperAttemptLogLevel.Warning,
                $"Synchronization '{action.Synchronization}' at '{page.Url}' was canceled.");
            throw;
        }
        catch (TimeoutException ex)
        {
            context.AddLog(ScraperAttemptLogLevel.Warning,
                $"Timed out waiting for synchronization '{action.Synchronization}' at '{page.Url}': {ex.Message}");
        }
        catch (Exception ex)
        {
            context.AddLog(ScraperAttemptLogLevel.Warning,
                $"Failed synchronization '{action.Synchronization}' at '{page.Url}': {ex.Message}");
        }

    }

    private async Task<bool> PerformPagination(
        IPage page,
        ScraperDefinition def,
        ScrapeContext context,
        string pageUrl
    )
    {
        if (def.PaginationType == PaginationType.None) return false;

        if (def.PaginationType == PaginationType.InfiniteScroll)
        {
            try
            {
                var advanced = await PerformScroll(page);

                if (advanced)
                {
                    context.AddLog(ScraperAttemptLogLevel.Information, $"advanced infinite-scroll pagination.");
                }

                return advanced;
            }
            catch (OperationCanceledException)
            {
                context.AddLog(ScraperAttemptLogLevel.Warning,
                    $"Infinite-scroll pagination on page '{pageUrl}' was canceled.");
                throw;
            }
            catch (Exception ex)
            {
                context.AddLog(ScraperAttemptLogLevel.Warning,
                    $"Failed to advance infinite-scroll pagination: {ex.Message}");
                throw;
            }
        }

        if (def.PaginationType == PaginationType.NextButton)
        {
            var nextBtnDef = def.Actions.FirstOrDefault(s => s.IsPaginationTrigger);
            if (nextBtnDef == null) return false;

            try
            {
                var nextBtn = await page.QuerySelectorAsync(nextBtnDef.Selector);
                if (nextBtn == null || await nextBtn.IsDisabledAsync()) return false;

                await nextBtn.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                await PerformWait(page, nextBtnDef.Delay);

                context.AddLog(ScraperAttemptLogLevel.Information,
                    $"advanced next-button pagination.");
                return true;
            }
            catch (OperationCanceledException)
            {
                context.AddLog(ScraperAttemptLogLevel.Warning,
                    $"Next-button pagination on page '{pageUrl}' was canceled.");
                throw;
            }
            catch (Exception ex)
            {
                context.AddLog(ScraperAttemptLogLevel.Warning,
                    $"Failed to advance next-button pagination: {ex.Message}");
                return false;
            }
        }

        return false;
    }

    private static async Task<bool> PerformScroll(
    IPage page,
    IElementHandle? container = null,
    int growthTimeoutMs = 8000,
    int settleMs = 600
    )
    {
        const string heightJs = "el => (el ?? document.scrollingElement).scrollHeight";
        // Scrolls one step and reports whether we're at the (live) bottom
        const string stepJs = @"el => {
        const e = el ?? document.scrollingElement;
        e.scrollBy({ top: e.clientHeight * 0.9 });
        return e.scrollTop + e.clientHeight >= e.scrollHeight - 2;
    }";
        const string nudgeJs = @"el => {
        const e = el ?? document.scrollingElement;
        e.scrollBy({ top: -400 });
        setTimeout(() => e.scrollTo({ top: e.scrollHeight }), 150);
    }";

        Task<double> Height() => page.EvaluateAsync<double>(heightJs, container);

        var vp = page.ViewportSize;
        await page.Mouse.MoveAsync((vp?.Width ?? 1280) / 2, (vp?.Height ?? 720) / 2);

        double start = await Height();

        // 1. Walk to the current bottom so everything already on the page renders
        for (int i = 0; i < 100; i++)
        {
            await page.Mouse.WheelAsync(0, 600);
            bool atBottom = await page.EvaluateAsync<bool>(stepJs, container);
            await Task.Delay(100);
            if (atBottom) break;
        }

        // 2. Wait for the page to grow, nudging the bottom if nothing happens
        var deadline = DateTime.UtcNow.AddMilliseconds(growthTimeoutMs);
        double height = await Height();
        int polls = 0;

        while (height <= start && DateTime.UtcNow < deadline)
        {
            await Task.Delay(200);
            height = await Height();

            if (height <= start && ++polls % 8 == 0)   // roughly every 1.6s
                await page.EvaluateAsync(nudgeJs, container);
        }

        if (height <= start)
            return false;

        // 3. Grew: wait until it stops changing so the caller sees a stable page
        var lastChange = DateTime.UtcNow;
        while ((DateTime.UtcNow - lastChange).TotalMilliseconds < settleMs)
        {
            await Task.Delay(100);
            double h = await Height();
            if (h != height)
            {
                height = h;
                lastChange = DateTime.UtcNow;
            }
        }

        return true;
    }

    private static async Task PerformWait(IPage page, int? delay)
    {
        if (delay.HasValue && delay.Value > 0)
        {
            await page.WaitForTimeoutAsync(delay.Value);
        }
    }
}
