using Microsoft.Playwright;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Engine.Models;

namespace Tendril.Engine.Scrapers.Dynamic;

public partial class DynamicScraper
{
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
}
