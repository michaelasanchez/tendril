using System.Runtime.CompilerServices;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Core.Interfaces.Repositories;
using Tendril.Engine.Abstractions;
using Tendril.Engine.Extensions;
using Tendril.Engine.Models;

namespace Tendril.Engine.Runtime;

public class ScrapeExecutor(
    DynamicScraper dynamicLogic,
    StaticScraper staticLogic,
    IScraperRepository repo,
    ScrapeResourceManager resources) : IScrapeExecutor
{
    public async IAsyncEnumerable<RawScrapedData> RunScraperAsync(
        ScraperDefinition def,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var context = new ScrapeContext();

        // Start the root scraper
        if (def.ExecutionMode == ExecutionMode.Static)
        {
            await foreach (var item in RunStaticPipelineAsync(def, context, ct)) // Context is null for root static
                yield return item;
        }
        else
        {
            await foreach (var item in RunDynamicPipelineAsync(def, context, ct)) // Context is null, will create new
                yield return item;
        }
    }

    // --- PIPELINE 1: STATIC ---
    private async IAsyncEnumerable<RawScrapedData> RunStaticPipelineAsync(
        ScraperDefinition def,
        ScrapeContext context,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // 1. Resolve Client
        var client = resources.EnsureClient(context);

        // 2. Execute (Now passing the client, not the HTML string)
        await foreach (var item in staticLogic.ExecuteAsync(client, def, ct))
        {
            if (item.ChildUrl is not null)
            {
                var childResults = new List<RawScrapedData>();

                try
                {
                    // Pass the 'client' down as the 'existingClient'
                    await foreach (var childEvent in RunChildDispatchAsync(item, context, ct))
                    {
                        childResults.Add(childEvent);
                    }
                }
                catch (Exception ex)
                {
                    // Log error
                }

                foreach (var res in childResults)
                {
                    yield return item.Data.MergeData(res); // Don't forget to merge!
                }
            }
            else
            {
                yield return item.Data;
            }
        }

        // Note: We do NOT dispose the HttpClient here because it might be owned 
        // by the Factory or the Parent. HttpClient is designed to be long-lived.
    }

    // --- PIPELINE 2: DYNAMIC ---
    private async IAsyncEnumerable<RawScrapedData> RunDynamicPipelineAsync(
        ScraperDefinition def,
        ScrapeContext context,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await using var scope = await resources.AcquireBrowserScopeAsync(context);

        var page = await scope.Browser.NewPageAsync();

        try
        {
            await foreach (var item in dynamicLogic.ExecuteAsync(page, def).WithCancellation(ct))
            {
                if (item.ChildUrl is not null)
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
                        // Log error
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
            // Close the tab
            await page.CloseAsync();
        }
    }

    // --- THE UNIFIED DISPATCHER ---
    private async IAsyncEnumerable<RawScrapedData> RunChildDispatchAsync(
        ScrapeYieldItem parentItem,
        ScrapeContext context,
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