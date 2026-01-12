using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Engine.Abstractions;
using Tendril.Engine.Extensions;
using Tendril.Engine.Models;

namespace Tendril.Engine.Runtime;

public class StaticScraper(IJsonLdProcessor jsonLd)
{
    // CHANGED: We now accept HttpClient instead of "string html"
    public async IAsyncEnumerable<ScrapeYieldItem> ExecuteAsync(
        HttpClient client,
        ScraperDefinition def,
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
                var data = jsonLd.Extract(html, def.Selectors.FirstOrDefault()?.Selector ?? "Event");
                if (data != null) yield return new ScrapeYieldItem { Data = data };
                // JsonLD usually doesn't have "Next Page" buttons in the DOM easily, 
                // but if it does, the logic below handles it. 
                // If not, we break to avoid infinite loop on same URL.
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
                foreach (var item in ExtractDomItems(page, def, currentUrl))
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

    private IEnumerable<ScrapeYieldItem> ExtractDomItems(HtmlDocument page, ScraperDefinition def, string currentUrl)
    {
        // 1. Pre-Scrape (Header data, etc)
        var container = def.Selectors.SingleOrDefault(x => x.Type == SelectorType.Container);
        if (container == null) yield break;

        var preSelectors = def.Selectors
            .Where(x => x.Order < container.Order && x.Type != SelectorType.Container)
            .OrderBy(x => x.Order);

        var preResult = new ScrapeYieldItem();

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

            var fieldSelectors = def.Selectors
                .Where(x =>
                    x.Type != SelectorType.Container &&
                    x.Order > container.Order &&
                    !x.IsPaginationTrigger)
                .OrderBy(x => x.Order);

            foreach (var step in fieldSelectors)
            {
                if (step.ChildScraperDefinitionId.HasValue)
                {
                    var targetNode = FindNode(item, step, def.ExtractionStrategy);

                    var url = targetNode?.GetAttributeValue("href", "");

                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        result = result with { ChildUrl = url, ChildScraperId = step.ChildScraperDefinitionId };
                    }
                }
                else
                {
                    ExtractField(page, item, step, result.Data, def.ExtractionStrategy, currentUrl);
                }
            }

            yield return preResult.Merge(result);
        }
    }

    private string? GetNextPageUrl(HtmlDocument page, ScraperDefinition def, string currentUrl)
    {
        if (def.PaginationType != PaginationType.NextButton) return null;

        var nextBtnDef = def.Selectors.FirstOrDefault(s => s.IsPaginationTrigger);

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
    private static HtmlNode? FindNode(HtmlNode scope, ScraperSelector step, ExtractionStrategy strategy)
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

    private static void ExtractField(HtmlDocument page, HtmlNode? parentNode, ScraperSelector step, RawScrapedData rawEvent, ExtractionStrategy strategy, string currentUrl)
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
                SelectorType.Text => targetNode.InnerText.Trim(),
                SelectorType.Attribute => targetNode.GetAttributeValue(step.AttributeName ?? "", ""),
                _ => null
            };

            // FIX: Check for relative URLs on href/src attributes
            if (step.Type == SelectorType.Attribute &&
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

            if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(step.FieldName))
            {
                rawEvent.Fields[step.FieldName] = value;
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
        var containerDef = def.Selectors.SingleOrDefault(x => x.Type == SelectorType.Container);
        if (containerDef == null) yield break;

        var matches = Regex.Matches(html, containerDef.Selector, RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            var result = new ScrapeYieldItem();

            var fieldSelectors = def.Selectors.Where(x => x.Type != SelectorType.Container);

            foreach (var step in fieldSelectors)
            {
                if (string.IsNullOrWhiteSpace(step.Selector))
                {
                    if (match.Groups.ContainsKey(step.FieldName))
                    {
                        result.Data.Fields[step.FieldName] = match.Groups[step.FieldName].Value;
                    }
                }
                else
                {
                    var subMatch = Regex.Match(match.Value, step.Selector);
                    if (subMatch.Success)
                    {
                        var val = subMatch.Groups.Count > 1 ? subMatch.Groups[1].Value : subMatch.Value;
                        result.Data.Fields[step.FieldName] = val;
                    }
                }
            }

            yield return result;
        }
    }
}