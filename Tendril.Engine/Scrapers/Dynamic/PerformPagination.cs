using Microsoft.Playwright;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Engine.Models;

namespace Tendril.Engine.Scrapers.Dynamic;

public partial class DynamicScraper
{
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

}
