using Microsoft.Playwright;
using System.Runtime.CompilerServices;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Core.Interfaces.Repositories;
using Tendril.Engine.Abstractions;
using Tendril.Engine.Extensions;
using Tendril.Engine.Models;
using Tendril.Engine.Scrapers;

namespace Tendril.Engine.Runtime;

public class ScrapeExecutor(
    DynamicScraper dynamicLogic,
    StaticScraper staticLogic,
    ApiScraper apiLogic,
    IScraperRepository repo,
    ScrapeResourceManager resources) : IScrapeExecutor
{
    public async IAsyncEnumerable<RawScrapedData> RunScraperAsync(
        ScraperDefinition def,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var context = new ScrapeContext();

        switch (def.ExecutionMode)
        {
            case ExecutionMode.Static:
            {

                await foreach (var item in RunStaticPipelineAsync(def, context, ct))
                    yield return item;

                break;
            }


            case ExecutionMode.Dynamic:
            {
                await foreach (var item in RunDynamicPipelineAsync(def, context, ct))
                    yield return item;

                break;
            }

            case ExecutionMode.Api:
            {
                await foreach (var item in RunApiPipelineAsync(def, context, ct))
                    yield return item;

                break;
            }
        }
    }

    // --- PIPELINE 1: STATIC --- //
    private async IAsyncEnumerable<RawScrapedData> RunStaticPipelineAsync(
        ScraperDefinition def,
        ScrapeContext context,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var client = resources.ResolveClient(context);

        await foreach (var item in staticLogic.ExecuteAsync(client, def, context, ct))
        {
            if (item.ChildScraperId is not null)
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
                    yield return item.Data.MergeData(res);
                }
            }
            else
            {
                yield return item.Data;
            }
        }
    }

    // --- PIPELINE 2: DYNAMIC --- //
    private async IAsyncEnumerable<RawScrapedData> RunDynamicPipelineAsync(
        ScraperDefinition def,
        ScrapeContext context,
        [EnumeratorCancellation] CancellationToken ct)
    {
        IPage? page = null;

        await using var scope = await resources.ResolveBrowserScope(context, def);

        page = def.UseHeadlessBrowser
            ? await scope.BrowserContext.NewPageAsync()
            : scope.GetPage() ?? await scope.BrowserContext.NewPageAsync();

        try
        {
            await foreach (var item in dynamicLogic.ExecuteAsync(page, def, context).WithCancellation(ct))
            {
                if (item.ChildScraperId is not null)
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
            try
            {
                await page.CloseAsync().WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (TimeoutException)
            {
                // page.Close hung, likely a stale CDP connection - continue cleanup
            }
            catch (Exception)
            {
                // ignore any other close errors, the context dispose will handle it
            }
        }
    }

    // --- PIPELINE 3: API --- //
    private async IAsyncEnumerable<RawScrapedData> RunApiPipelineAsync(
        ScraperDefinition def,
        ScrapeContext context,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var client = resources.ResolveClient(context);

        await foreach (var item in apiLogic.ExecuteAsync(client, def, context, ct))
        {
            if (item.ChildScraperId is not null)
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

    // --- CHILD DISPATCHER ---
    private async IAsyncEnumerable<RawScrapedData> RunChildDispatchAsync(
        ScrapeYieldItem parentItem,
        ScrapeContext context,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var childDef = await repo.GetByIdWithDetailsAsync(parentItem.ChildScraperId!.Value, ct);

        if (childDef == null) yield break;

        var previousBaseUrl = childDef.BaseUrl; // Store previous base URL to restore later

        if (parentItem.ChildUrl is not null)
        {
            childDef.BaseUrl = parentItem.ChildUrl;
        }

        context.ParentItem = parentItem;

        IAsyncEnumerable<RawScrapedData> pipeline = childDef.ExecutionMode switch
        {
            ExecutionMode.Static => RunStaticPipelineAsync(childDef, context, ct),
            ExecutionMode.Dynamic => RunDynamicPipelineAsync(childDef, context, ct),
            ExecutionMode.Api => RunApiPipelineAsync(childDef, context, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(childDef.ExecutionMode))
        };

        await foreach (var res in pipeline)
        {
            yield return res;
        }

        childDef.BaseUrl = previousBaseUrl;
        context.ParentItem = null;
    }

}