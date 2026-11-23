using Microsoft.Playwright;
using System.Runtime.CompilerServices;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Engine.Extensions;
using Tendril.Engine.Interfaces;
using Tendril.Engine.Models;

namespace Tendril.Engine.Scrapers.Dynamic;

public partial class DynamicScraper(IJsonLdProcessor jsonLd)
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
                    switch (action.Type)
                    {


                    }
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

    private static async Task PerformWait(IPage page, int? delay)
    {
        if (delay.HasValue && delay.Value > 0)
        {
            await page.WaitForTimeoutAsync(delay.Value);
        }
    }
}
