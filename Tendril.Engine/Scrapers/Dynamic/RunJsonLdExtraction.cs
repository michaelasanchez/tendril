using Microsoft.Playwright;
using System.Runtime.CompilerServices;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Engine.Models;

namespace Tendril.Engine.Scrapers.Dynamic;

public partial class DynamicScraper
{
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

}
