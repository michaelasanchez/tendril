using Microsoft.Playwright;
using System.Runtime.CompilerServices;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Core.Interfaces.Repositories;
using Tendril.Engine.Abstractions;
using Tendril.Engine.Extensions;
using Tendril.Engine.Models;
using Tendril.Engine.Playwright;

namespace Tendril.Engine.Runtime;

public class ScrapeExecutor(
    DynamicScraper dynamicLogic,
    StaticScraper staticLogic,
    IHttpClientFactory httpClientFactory,
    IScraperRepository repo) : IScrapeExecutor
{
    public async IAsyncEnumerable<RawScrapedData> RunScraperAsync(
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
    private async IAsyncEnumerable<RawScrapedData> RunStaticPipelineAsync(
        ScraperDefinition def,
        IBrowserContext? parentContext, // Passed down just in case a child needs it
        [EnumeratorCancellation] CancellationToken ct)
    {
        // Use Typed Client if you set it up, otherwise use Factory
        var client = httpClientFactory.CreateClient("ScraperClient");

        var html = await client.GetStringAsync(def.BaseUrl, ct);

        await foreach (var item in staticLogic.ExecuteAsync(html, def, ct))
        {
            if (item.ChildUrl is not null)
            {
                var childResults = new List<RawScrapedData>();

                try
                {
                    await foreach (var childEvent in RunChildDispatchAsync(item, parentContext, ct))
                    {
                        childResults.Add(childEvent);
                    }
                }
                catch (Exception ex)
                {
                    // TODO: we'll figure this out one day
                }

                foreach (var res in childResults)
                {
                    yield return res;
                }
            }
            else
            {
                yield return item.Data;
            }
        }
    }

    // --- PIPELINE 2: DYNAMIC ---
    private async IAsyncEnumerable<RawScrapedData> RunDynamicPipelineAsync(
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

        try
        {
            await page.GotoAsync(def.BaseUrl);

            await foreach (var item in dynamicLogic.ExecuteAsync(page, def).WithCancellation(ct))
            {
                if (item.ChildUrl != null)
                {
                    var childResults = new List<RawScrapedData>();

                    try
                    {
                        await foreach (var childEvent in RunChildDispatchAsync(item, context, ct))
                        {
                            childResults.Add(childEvent);
                        }
                    }
                    catch (Exception ex)
                    {
                        // TODO: we'll figure this out one day
                    }

                    foreach (var res in childResults)
                    {
                        yield return item.Data.MergeData(res);
                    }
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
    private async IAsyncEnumerable<RawScrapedData> RunChildDispatchAsync(
        ScrapeYieldItem parentItem,
        IBrowserContext? context,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var childDef = await repo.GetByIdWithDetailsAsync(parentItem.ChildScraperId!.Value, ct);

        if (childDef == null) yield break;

        childDef.BaseUrl = parentItem.ChildUrl!;

        IAsyncEnumerable<RawScrapedData> pipeline;

        if (childDef.ExecutionMode == ExecutionMode.Static)
        {
            pipeline = RunStaticPipelineAsync(childDef, context, ct);
        }
        else
        {
            pipeline = RunDynamicPipelineAsync(childDef, context, ct);
        }

        await foreach (var res in pipeline)
        {
            yield return res;
        }
    }
}