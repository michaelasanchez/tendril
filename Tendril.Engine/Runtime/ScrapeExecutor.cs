using Microsoft.Playwright;
using System.Runtime.CompilerServices;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Core.Interfaces.Repositories;
using Tendril.Engine.Abstractions;
using Tendril.Engine.Models;
using Tendril.Engine.Playwright;

namespace Tendril.Engine.Runtime;

public class ScrapeExecutor(
    DynamicScraper dynamicLogic,
    StaticScraper staticLogic,
    IHttpClientFactory httpClientFactory,
    IScraperRepository repo) : IScrapeExecutor
{
    public async IAsyncEnumerable<RawScrapedEvent> RunScraperAsync(
        ScraperDefinition def,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // Start the root scraper
        if (def.ExecutionMode == ExecutionMode.Static)
        {
            await foreach (var item in RunStaticPipelineAsync(def, null, ct)) // Context is null for root static
                yield return item;
        }
        else
        {
            await foreach (var item in RunDynamicPipelineAsync(def, null, ct)) // Context is null, will create new
                yield return item;
        }
    }

    // --- PIPELINE 1: STATIC ---
    private async IAsyncEnumerable<RawScrapedEvent> RunStaticPipelineAsync(
        ScraperDefinition def,
        IBrowserContext? parentContext, // Passed down just in case a child needs it
        [EnumeratorCancellation] CancellationToken ct)
    {
        // Use Typed Client if you set it up, otherwise use Factory
        var client = httpClientFactory.CreateClient("ScraperClient");

        var html = await client.GetStringAsync(def.BaseUrl, ct);

        await foreach (var item in staticLogic.ExecuteAsync(html, def, ct))
        {
            if (item.ChildUrl != null)
            {
                // RECURSION: Static Parent found a child
                var childEvent = await RunChildDispatchAsync(item, parentContext, ct);
                if (childEvent != null) yield return MergeEvents(item.Data, childEvent);
            }
            else
            {
                yield return item.Data;
            }
        }
    }

    // --- PIPELINE 2: DYNAMIC ---
    private async IAsyncEnumerable<RawScrapedEvent> RunDynamicPipelineAsync(
        ScraperDefinition def,
        IBrowserContext? existingContext, // Reuse if available
        [EnumeratorCancellation] CancellationToken ct)
    {
        // Reuse context if passed (from a Dynamic Parent), otherwise create new
        var context = existingContext ?? await PlaywrightContextFactory.CreateContextAsync();

        // If we created it, we own it and must dispose it. 
        // If it was passed in, the parent owns it.
        bool isContextOwner = existingContext == null;

        var page = await context.NewPageAsync();

        // TOOD: this should be set up as configuration
        //  ScraperDefinition.BlockedMediaTypes or something like that
        //await page.RouteAsync("**/*", async route =>
        //{
        //    var type = route.Request.ResourceType;
        //    if (type == "image" || type == "stylesheet" || type == "font" || type == "media")
        //    {
        //        await route.AbortAsync();
        //    }
        //    else
        //    {
        //        await route.ContinueAsync();
        //    }
        //});

        try
        {
            await page.GotoAsync(def.BaseUrl);

            await foreach (var item in dynamicLogic.ExecuteAsync(page, def).WithCancellation(ct))
            {
                if (item.ChildUrl != null)
                {
                    // RECURSION: Dynamic Parent found a child
                    // We pass 'context' down so dynamic children can share the session
                    var childEvent = await RunChildDispatchAsync(item, context, ct);

                    if (childEvent != null) yield return MergeEvents(item.Data, childEvent);
                }
                else
                {
                    yield return item.Data;
                }
            }
        }
        finally
        {
            await page.CloseAsync(); // Close the tab

            if (isContextOwner) await context.DisposeAsync(); // Close the browser if we opened it
        }
    }

    // --- THE UNIFIED DISPATCHER ---
    private async Task<RawScrapedEvent?> RunChildDispatchAsync(
        ScrapeYieldItem parentItem,
        IBrowserContext? context,
        CancellationToken ct)
    {
        var childDef = await repo.GetByIdAsync(parentItem.ChildScraperId!.Value, ct);

        if (childDef == null) return null;

        childDef.BaseUrl = parentItem.ChildUrl!;

        // DECISION POINT: Switch based on the CHILD'S mode
        if (childDef.ExecutionMode == ExecutionMode.Static)
        {
            await foreach (var res in RunStaticPipelineAsync(childDef, context, ct))
            {
                return res; // Return first result
            }
        }
        else
        {
            await foreach (var res in RunDynamicPipelineAsync(childDef, context, ct))
            {
                return res; // Return first result
            }
        }

        return null;
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