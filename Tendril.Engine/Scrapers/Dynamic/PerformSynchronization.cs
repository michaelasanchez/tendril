using Microsoft.Playwright;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Engine.Models;

namespace Tendril.Engine.Scrapers.Dynamic;

public partial class DynamicScraper
{
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

}
