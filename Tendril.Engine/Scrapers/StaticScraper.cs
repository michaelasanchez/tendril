using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Engine.Extensions;
using Tendril.Engine.Interfaces;
using Tendril.Engine.Models;

namespace Tendril.Engine.Scrapers;

public class StaticScraper(IJsonLdProcessor jsonLd)
{
    // CHANGED: We now accept HttpClient instead of "string html"
    public async IAsyncEnumerable<ScrapeYieldItem> ExecuteAsync(
        HttpClient client,
        ScraperDefinition def,
        ScrapeContext context,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var currentUrl = def.BaseUrl;
        var processedUrls = new HashSet<string>(); // Prevent infinite loops
        bool hasMore = true;

        // THE MAIN LOOP
        while (hasMore && !string.IsNullOrEmpty(currentUrl) && !processedUrls.Contains(currentUrl))
        {
            processedUrls.Add(currentUrl);

            // 1. FETCH
            string html;
            try
            {
                html = await client.GetStringAsync(currentUrl, ct);
            }
            catch
            {
                // Log failure? Stop? For now, we break the loop.
                yield break;
            }

            // 2. STRATEGY: JSON-LD
            if (def.ExtractionStrategy == ExtractionStrategy.JsonLd)
            {
                foreach (var data in jsonLd.ExtractAll(html, def.Actions.FirstOrDefault()?.Selector ?? "Event"))
                    yield return new ScrapeYieldItem { Data = data };

                if (def.PaginationType == PaginationType.None) yield break;
            }

            // 3. STRATEGY: REGEX
            else if (def.ExtractionStrategy == ExtractionStrategy.Regex)
            {
                foreach (var item in RunRegexExtraction(html, def))
                {
                    yield return item;
                }
            }

            // 4. STRATEGY: DOM (CSS/XPath)
            else
            {
                var page = new HtmlDocument();

                page.LoadHtml(html);

                // A. Extract Items (Logic extracted to helper for cleanliness)
                foreach (var item in ExtractDomItems(page, def, context, currentUrl))
                {
                    yield return item;
                }

                // B. Handle Pagination
                // We overwrite currentUrl if we find a new link, otherwise null stops the loop
                currentUrl = GetNextPageUrl(page, def, currentUrl);

                if (!string.IsNullOrEmpty(currentUrl))
                {
                    await Task.Delay(Random.Shared.Next(1000, 2000), ct);
                }
            }

            // Global Check: If we didn't get a new URL, stop.
            if (string.IsNullOrEmpty(currentUrl)) hasMore = false;
        }
    }

    private IEnumerable<ScrapeYieldItem> ExtractDomItems(HtmlDocument page, ScraperDefinition def, ScrapeContext context, string currentUrl)
    {
        // 1. Pre-Scrape (Header data, etc)
        var container = def.Actions.SingleOrDefault(x => x.Type == ActionType.Container);
        if (container == null) yield break;

        var preSelectors = def.Actions
            .Where(x => x.Order < container.Order && x.Type != ActionType.Container)
            .OrderBy(x => x.Order);

        var preResult = (context.ParentItem ?? new ScrapeYieldItem()) with
        {
            ChildScraperId = null,
            ChildUrl = null,
        };

        foreach (var step in preSelectors)
        {
            ExtractField(page, null, step, preResult.Data, def.ExtractionStrategy, currentUrl);
        }

        // 2. Find Items
        IEnumerable<HtmlNode>? items = null;
        if (def.ExtractionStrategy == ExtractionStrategy.XPath)
            items = page.DocumentNode.SelectNodes(container.Selector);
        else
            items = page.DocumentNode.QuerySelectorAll(container.Selector);

        if (items == null) yield break;

        // 3. Loop Items
        foreach (var item in items)
        {
            var result = new ScrapeYieldItem();

            var partial = false;

            var fieldSelectors = def.Actions
                .Where(x =>
                    x.Type != ActionType.Container &&
                    x.Order > container.Order &&
                    !x.IsPaginationTrigger)
                .OrderBy(x => x.Order);

            foreach (var step in fieldSelectors)
            {
                if (step.Type == ActionType.FollowLink && step.ChildScraperDefinitionId.HasValue)
                {
                    var targetNode = FindNode(item, step, def.ExtractionStrategy);

                    var url = targetNode?.GetAttributeValue("href", "");

                    result.Data.Fields[step.OutputField] = url;

                    if (!string.IsNullOrWhiteSpace(url) && (step.AllowDuplicateUrls is true || !context.HasVisited(url)))
                    {
                        context.MarkVisited(url);

                        result = result with
                        {
                            ChildScraperId = step.ChildScraperDefinitionId,
                            ChildUrl = url,
                            AllowEmptyResult = step.AllowDuplicateUrls
                        };
                    }
                    else
                    {
                        partial = true;
                        break; // Stop processing this specific list item
                    }
                }
                else if (step.Type == ActionType.CallApi && step.ChildScraperDefinitionId.HasValue)
                {
                    result = result with
                    {
                        ChildScraperId = step.ChildScraperDefinitionId,
                        AllowDuplicateUrls = step.AllowDuplicateUrls,
                        AllowEmptyResult = step.AllowEmptyResult
                    };
                }
                else
                {
                    ExtractField(page, item, step, result.Data, def.ExtractionStrategy, currentUrl);
                }
            }

            if (!partial)
            {
                yield return preResult.Merge(result);
            }
        }


        if (!items.Any() && context.ParentItem?.AllowEmptyResult is true)
        {
            yield return preResult; // Yield what we have, even if it's empty
        }
    }

    private static string? GetNextPageUrl(HtmlDocument page, ScraperDefinition def, string currentUrl)
    {
        if (def.PaginationType != PaginationType.NextButton) return null;

        var nextBtnDef = def.Actions.FirstOrDefault(s => s.IsPaginationTrigger);

        if (nextBtnDef == null) return null;

        HtmlNode? nextBtn = null;

        if (def.ExtractionStrategy == ExtractionStrategy.XPath)
            nextBtn = page.DocumentNode.SelectSingleNode(nextBtnDef.Selector);
        else
            nextBtn = page.DocumentNode.QuerySelector(nextBtnDef.Selector);

        if (nextBtn == null) return null;

        // 1. Get the raw value
        var rawHref = nextBtn.GetAttributeValue("href", null);
        if (string.IsNullOrWhiteSpace(rawHref)) return null;

        // 2. FIX: Decode HTML entities (converts &amp; -> &, &quot; -> ", etc.)
        var href = System.Net.WebUtility.HtmlDecode(rawHref);

        //// 3. Cleanup & Validation (The logic we added earlier)
        //if (href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
        //    href.StartsWith("#") ||
        //    href.StartsWith("mailto:"))
        //    return null;

        // 4. Combine with Base URL
        return new Uri(new Uri(currentUrl), href).ToString();
    }

    // Helper to switch between CSS/XPath logic for finding nodes
    private static HtmlNode? FindNode(HtmlNode scope, ScraperAction step, ExtractionStrategy strategy)
    {
        if (string.IsNullOrWhiteSpace(step.Selector)) return scope;

        if (strategy == ExtractionStrategy.XPath)
        {
            return scope.SelectSingleNode(step.Selector);
        }
        else
        {
            return scope.QuerySelector(step.Selector);
        }
    }

    private static void ExtractField(HtmlDocument page, HtmlNode? parentNode, ScraperAction step, RawScrapedData rawEvent, ExtractionStrategy strategy, string currentUrl)
    {
        try
        {
            HtmlNode? targetNode = null;

            if (step.Root || parentNode is null)
            {
                targetNode = strategy == ExtractionStrategy.XPath
                    ? page.DocumentNode.SelectSingleNode(step.Selector)
                    : page.DocumentNode.QuerySelector(step.Selector);
            }
            else
            {
                targetNode = FindNode(parentNode, step, strategy);
            }

            if (targetNode == null) return;

            string? value = step.Type switch
            {
                ActionType.Text => targetNode.InnerText.Trim(),
                ActionType.Attribute => targetNode.GetAttributeValue(step.AttributeName ?? "", ""),
                ActionType.ConstantValue => step.ConstantValue,
                _ => null
            };

            // FIX: Check for relative URLs on href/src attributes
            if (step.Type == ActionType.Attribute &&
                (step.AttributeName == "href" || step.AttributeName == "src") &&
                !string.IsNullOrWhiteSpace(value))
            {
                // Decode first (handle &amp;)
                value = System.Net.WebUtility.HtmlDecode(value);

                // If it's relative (starts with /), combine it
                if (Uri.TryCreate(value, UriKind.Relative, out _))
                {
                    try
                    {
                        value = new Uri(new Uri(currentUrl), value).ToString();
                    }
                    catch { /* fallback to original value if Uri fails */ }
                }
            }

            if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(step.OutputField))
            {
                rawEvent.Fields[step.OutputField] = value;
            }
        }
        catch
        {
            // TODO: we'll figure this out one day
        }
    }

    // REGEX IMPLEMENTATION
    private IEnumerable<ScrapeYieldItem> RunRegexExtraction(string html, ScraperDefinition def)
    {
        var containerDef = def.Actions.SingleOrDefault(x => x.Type == ActionType.Container);
        if (containerDef == null) yield break;

        var matches = Regex.Matches(html, containerDef.Selector, RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            var result = new ScrapeYieldItem();

            var fieldSelectors = def.Actions.Where(x => x.Type != ActionType.Container);

            foreach (var step in fieldSelectors)
            {
                if (string.IsNullOrWhiteSpace(step.Selector))
                {
                    if (match.Groups.ContainsKey(step.OutputField))
                    {
                        result.Data.Fields[step.OutputField] = match.Groups[step.OutputField].Value;
                    }
                }
                else
                {
                    var subMatch = Regex.Match(match.Value, step.Selector);
                    if (subMatch.Success)
                    {
                        var val = subMatch.Groups.Count > 1 ? subMatch.Groups[1].Value : subMatch.Value;
                        result.Data.Fields[step.OutputField] = val;
                    }
                }
            }

            yield return result;
        }
    }
}