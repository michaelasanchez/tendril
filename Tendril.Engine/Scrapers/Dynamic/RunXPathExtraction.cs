using Microsoft.Playwright;
using System.Runtime.CompilerServices;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Engine.Extensions;
using Tendril.Engine.Models;

namespace Tendril.Engine.Scrapers.Dynamic;

public partial class DynamicScraper
{
    private async IAsyncEnumerable<ScrapeYieldItem> RunXPathExtraction(
        IPage page,
        ScraperDefinition definition,
        ScrapeContext context,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var containerAction = definition.Actions.SingleOrDefault(x => x.Type == ActionType.Container);
        if (containerAction is null)
        {
            context.AddLog(ScraperAttemptLogLevel.Warning,
                "XPath extraction requires a container action.");
            yield break;
        }

        var preActions = definition.Actions
            .Where(x => x.Order < containerAction.Order && x.Type != ActionType.Container)
            .OrderBy(x => x.Order);

        var preResult = (context.ParentItem ?? new ScrapeYieldItem()) with
        {
            ChildScraperId = null,
            ChildUrl = null,
        };

        foreach (var action in preActions)
        {
            ct.ThrowIfCancellationRequested();

            await PerformXPathAction(page, null, action, preResult.Data, context, ct);
        }

        try
        {
            await page.Locator(ToXPathSelector(containerAction.Selector)).First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Attached,
                Timeout = containerAction.Delay ?? 10000
            });
        }
        catch (TimeoutException)
        {
            context.AddLog(ScraperAttemptLogLevel.Warning,
                $"Timed out waiting for XPath container '{containerAction.Selector}' at '{page.Url}'. Waited ${containerAction.Delay ?? 10000}ms.");
            yield break;
        }

        var processedSignatures = new HashSet<string>();
        var hasMore = true;

        do
        {
            ct.ThrowIfCancellationRequested();

            var pageUrl = page.Url;
            var container = page.Locator(ToXPathSelector(containerAction.Selector));
            var itemCount = await container.CountAsync();

            context.AddLog(ScraperAttemptLogLevel.Information,
                $"Selected {itemCount} XPath item(s) on page '{pageUrl}'.");

            for (var i = 0; i < itemCount; i++)
            {
                ct.ThrowIfCancellationRequested();

                var item = container.Nth(i);
                var result = new ScrapeYieldItem();
                var partial = false;

                var itemActions = definition.Actions
                    .Where(x => x.Type != ActionType.Container &&
                                x.Order > containerAction.Order &&
                                !x.IsPaginationTrigger)
                    .OrderBy(x => x.Order);

                foreach (var action in itemActions)
                {
                    ct.ThrowIfCancellationRequested();

                    if (action.Type == ActionType.FollowLink && action.ChildScraperDefinitionId.HasValue)
                    {
                        var link = string.IsNullOrWhiteSpace(action.Selector)
                            ? item
                            : item.Locator(ToXPathSelector(action.Selector)).First;

                        if (await link.CountAsync() == 0)
                        {
                            partial = true;
                            break;
                        }

                        var rawUrl = await link.GetAttributeAsync("href");
                        var url = ResolveUrl(page.Url, rawUrl);

                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            result.Data.Fields[action.OutputField] = url;
                        }

                        if (!string.IsNullOrWhiteSpace(url) &&
                            (action.AllowDuplicateUrls is true || !context.HasVisited(url)))
                        {
                            context.MarkVisited(url);
                            result = result with
                            {
                                ChildScraperId = action.ChildScraperDefinitionId,
                                ChildUrl = url,
                                AllowDuplicateUrls = action.AllowDuplicateUrls,
                                AllowEmptyResult = action.AllowEmptyResult
                            };
                        }
                        else
                        {
                            partial = true;
                            break;
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
                        await PerformXPathAction(page, item, action, result.Data, context, ct);
                    }
                }

                var signature = result.Data.GetSignature();
                if (!partial && processedSignatures.Add(signature))
                {
                    yield return preResult.Merge(result);
                }
            }

            if (itemCount == 0)
            {
                hasMore = false;
                context.AddLog(ScraperAttemptLogLevel.Information,
                    "No XPath items found.");

                if (context.ParentItem?.AllowEmptyResult == true)
                {
                    yield return preResult;
                }
            }
            else
            {
                if (definition.PaginationType == PaginationType.None)
                {
                    hasMore = false;
                }
                else
                {
                    hasMore = await PerformXPathPagination(page, definition, context, pageUrl, ct);
                    if (!hasMore)
                    {
                        context.AddLog(ScraperAttemptLogLevel.Warning,
                            $"Pagination attempt did not succeed. ({definition.PaginationType})");
                    }
                }
            }
        } while (hasMore);
    }

    private static async Task PerformXPathAction(
        IPage page,
        ILocator? item,
        ScraperAction action,
        RawScrapedData? scrapedData,
        ScrapeContext context,
        CancellationToken ct)
    {
        if (action.Type is ActionType.Container or ActionType.FollowLink or ActionType.CallApi)
        {
            return;
        }

        ct.ThrowIfCancellationRequested();
        
        if (action.Type == ActionType.Synchronize)
        {
            await PerformXPathSynchronization(page, item, action, context, ct);
        }

        try
        {
            ILocator? target = null;

            if (action.Root || item is null)
            {
                if (!string.IsNullOrWhiteSpace(action.Selector))
                {
                    target = page.Locator(ToXPathSelector(action.Selector)).First;
                }
            }
            else if (string.IsNullOrWhiteSpace(action.Selector))
            {
                target = item;
            }
            else
            {
                target = item.Locator(ToXPathSelector(action.Selector)).First;
            }

            if (target is null || await target.CountAsync() == 0)
            {
                return;
            }

            switch (action.Type)
            {
                case ActionType.Click:
                    await target.ClickAsync();
                    await PerformWait(page, action.Delay);
                    context.AddLog(ScraperAttemptLogLevel.Information,
                        $"Click on XPath '{action.Selector}' succeeded.");
                    return;

                case ActionType.Hover:
                    await target.HoverAsync();
                    await PerformWait(page, action.Delay);
                    return;

                case ActionType.Input:
                    if (!string.IsNullOrEmpty(action.InteractionValue))
                    {
                        await target.FillAsync(action.InteractionValue);
                        await PerformWait(page, action.Delay);
                    }
                    return;

                case ActionType.Scroll:
                    var element = await target.ElementHandleAsync();
                    await PerformScroll(page, element);
                    return;
            }

            if (scrapedData is null)
            {
                return;
            }

            string? value = null;

            if (action.Type == ActionType.Text)
            {
                value = await target.InnerTextAsync();
            }
            else if (action.Type == ActionType.Attribute)
            {
                value = await target.GetAttributeAsync(action.AttributeName ?? "");

                if (!string.IsNullOrWhiteSpace(value) &&
                    action.AttributeName is "href" or "src")
                {
                    value = ResolveUrl(page.Url, value);
                }
            }
            else if (action.Type == ActionType.ConstantValue)
            {
                value = action.ConstantValue;
            }

            if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(action.OutputField))
            {
                scrapedData.Fields[action.OutputField] = value.Trim();
            }
        }
        catch (OperationCanceledException)
        {
            context.AddLog(ScraperAttemptLogLevel.Warning,
                $"Processing XPath '{action.Selector}' at '{page.Url}' was canceled.");
            throw;
        }
        catch (TimeoutException ex)
        {
            context.AddLog(ScraperAttemptLogLevel.Warning,
                $"Timed out processing XPath '{action.Selector}' at '{page.Url}': {ex.Message}");
        }
        catch (Exception ex)
        {
            context.AddLog(ScraperAttemptLogLevel.Warning,
                $"Failed to process XPath '{action.Selector}' at '{page.Url}': {ex.Message}");
        }
    }

    private static async Task PerformXPathSynchronization(
        IPage page,
        ILocator? item,
        ScraperAction action,
        ScrapeContext context,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var timeout = action.Delay ?? 10000;

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

                    var locator = action.Root || item is null
                        ? page.Locator(ToXPathSelector(action.Selector))
                        : item.Locator(ToXPathSelector(action.Selector));

                    var state = action.Synchronization switch
                    {
                        SynchronizationCondition.SelectorAttached => WaitForSelectorState.Attached,
                        SynchronizationCondition.SelectorVisible => WaitForSelectorState.Visible,
                        _ => WaitForSelectorState.Hidden
                    };

                    await locator.WaitForAsync(new LocatorWaitForOptions
                    {
                        State = state,
                        Timeout = timeout
                    });
                    break;
                }

                case SynchronizationCondition.DomContentLoaded:
                    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded,
                        new PageWaitForLoadStateOptions { Timeout = timeout });
                    break;

                case SynchronizationCondition.Load:
                    await page.WaitForLoadStateAsync(LoadState.Load,
                        new PageWaitForLoadStateOptions { Timeout = timeout });
                    break;

                case SynchronizationCondition.NetworkIdle:
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle,
                        new PageWaitForLoadStateOptions { Timeout = timeout });
                    break;

                case SynchronizationCondition.JsonLdPresent:
                    await page.Locator("xpath=//script[@type='application/ld+json']")
                        .WaitForAsync(new LocatorWaitForOptions
                        {
                            State = WaitForSelectorState.Attached,
                            Timeout = timeout
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
            throw;
        }
        catch (TimeoutException ex)
        {
            context.AddLog(ScraperAttemptLogLevel.Warning,
                $"Timed out waiting for XPath synchronization '{action.Synchronization}' at '{page.Url}': {ex.Message}");
        }
        catch (Exception ex)
        {
            context.AddLog(ScraperAttemptLogLevel.Warning,
                $"Failed XPath synchronization '{action.Synchronization}' at '{page.Url}': {ex.Message}");
        }
    }

    private static async Task<bool> PerformXPathPagination(
        IPage page,
        ScraperDefinition definition,
        ScrapeContext context,
        string pageUrl,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (definition.PaginationType == PaginationType.InfiniteScroll)
        {
            try
            {
                var advanced = await PerformScroll(page);
                if (advanced)
                {
                    context.AddLog(ScraperAttemptLogLevel.Information,
                        "Advanced infinite-scroll pagination.");
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

        if (definition.PaginationType == PaginationType.NextButton)
        {
            var nextButtonAction = definition.Actions.FirstOrDefault(x => x.IsPaginationTrigger);
            if (nextButtonAction is null || string.IsNullOrWhiteSpace(nextButtonAction.Selector))
            {
                return false;
            }

            try
            {
                var nextButton = page.Locator(ToXPathSelector(nextButtonAction.Selector)).First;
                if (await nextButton.CountAsync() == 0 || await nextButton.IsDisabledAsync())
                {
                    return false;
                }

                await nextButton.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                await PerformWait(page, nextButtonAction.Delay);

                context.AddLog(ScraperAttemptLogLevel.Information,
                    "Advanced next-button pagination.");
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
                    $"Failed to advance XPath next-button pagination: {ex.Message}");
                return false;
            }
        }

        return false;
    }

    private static string ToXPathSelector(string? selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            return "xpath=//*";
        }

        return selector.StartsWith("xpath=", StringComparison.OrdinalIgnoreCase)
            ? selector
            : $"xpath={selector}";
    }

    private static string? ResolveUrl(string currentUrl, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var decoded = System.Net.WebUtility.HtmlDecode(value.Trim());
        if (Uri.TryCreate(decoded, UriKind.Absolute, out var absolute))
        {
            return absolute.ToString();
        }

        if (Uri.TryCreate(currentUrl, UriKind.Absolute, out var baseUri) &&
            Uri.TryCreate(baseUri, decoded, out var resolved))
        {
            return resolved.ToString();
        }

        return decoded;
    }
}
