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
    public async IAsyncEnumerable<ScrapeYieldItem> ExecuteAsync(
        string html,
        ScraperDefinition def,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // 1. JSON-LD STRATEGY
        if (def.ExtractionStrategy == ExtractionStrategy.JsonLd)
        {
            var data = jsonLd.Extract(html, def.Selectors.FirstOrDefault()?.Selector ?? "Event");

            if (data != null)
            {
                yield return new ScrapeYieldItem { Data = data };
            }

            yield break;
        }

        // 2. REGEX STRATEGY (Brute Force)
        if (def.ExtractionStrategy == ExtractionStrategy.Regex)
        {
            // Regex is usually "Global" (scan whole doc), not hierarchical (Container -> Item)
            // But we can still loop through matches.
            foreach (var item in RunRegexExtraction(html, def))
            {
                yield return item;
            }

            yield break;
        }

        // 3. DOM STRATEGIES (CSS or XPath)
        // Both require parsing the HTML first
        var page = new HtmlDocument();
        page.LoadHtml(html);



        // A. Find Containers
        var container = def.Selectors.SingleOrDefault(x => x.Type == SelectorType.Container);
        if (container == null) yield break;

        var preSelectors = def.Selectors
            .Where(x => x.Order < container.Order && x.Type != SelectorType.Container)
            .OrderBy(x => x.Order);

        var preResult = new ScrapeYieldItem();

        foreach (var step in preSelectors)
        {
            ExtractField(page, null, step, preResult.Data, def.ExtractionStrategy);
        }

        IEnumerable<HtmlNode>? items = null;

        if (def.ExtractionStrategy == ExtractionStrategy.XPath)
        {
            items = page.DocumentNode.SelectNodes(container.Selector);
        }
        else // CSS
        {
            items = page.DocumentNode.QuerySelectorAll(container.Selector);
        }

        if (items == null) yield break;

        foreach (var item in items)
        {
            var result = new ScrapeYieldItem();

            var fieldSelectors = def.Selectors
                .Where(x => x.Type != SelectorType.Container && !x.IsPaginationTrigger)
                .OrderBy(x => x.Order);

            foreach (var step in fieldSelectors)
            {
                // CHILD SCRAPER LOGIC
                if (step.ChildScraperDefinitionId.HasValue)
                {
                    // Helper determines how to find the node based on Strategy
                    var targetNode = FindNode(item, step, def.ExtractionStrategy);

                    var url = targetNode?.GetAttributeValue("href", "");

                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        result = result with { ChildUrl = url, ChildScraperId = step.ChildScraperDefinitionId };
                    }
                }
                // FIELD LOGIC
                else
                {
                    ExtractField(page, item, step, result.Data, def.ExtractionStrategy);
                }
            }

            yield return preResult.Merge(result);
        }
    }

    // Helper to switch between CSS/XPath logic for finding nodes
    private static HtmlNode? FindNode(HtmlNode scope, ScraperSelector step, ExtractionStrategy strategy)
    {
        if (string.IsNullOrWhiteSpace(step.Selector)) return scope;

        if (strategy == ExtractionStrategy.XPath)
        {
            // XPath needs specific dot-prefixing to be relative (e.g. "./div")
            return scope.SelectSingleNode(step.Selector);
        }
        else
        {
            return scope.QuerySelector(step.Selector);
        }
    }

    private static void ExtractField(HtmlDocument page, HtmlNode? parentNode, ScraperSelector step, RawScrapedData rawEvent, ExtractionStrategy strategy)
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
        // Regex strategy assumes the "Container" selector IS the Regex pattern.
        // It must contain a Named Group called "json" or be complex enough to map fields.
        // This is a simple implementation: 1 Container Regex, then sub-regexes for fields.

        var containerDef = def.Selectors.SingleOrDefault(x => x.Type == SelectorType.Container);
        if (containerDef == null) yield break;

        var matches = Regex.Matches(html, containerDef.Selector, RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            var result = new ScrapeYieldItem();

            // Loop through fields to see if we extract from the MATCH text
            var fieldSelectors = def.Selectors.Where(x => x.Type != SelectorType.Container);

            foreach (var step in fieldSelectors)
            {
                // If selector is empty, try to grab a Named Group from the container match
                if (string.IsNullOrWhiteSpace(step.Selector))
                {
                    // e.g. Container Regex:  <a href="(?<link>.*?)">(?<title>.*?)</a>
                    // Field "Title" maps to group "title"
                    if (match.Groups.ContainsKey(step.FieldName))
                    {
                        result.Data.Fields[step.FieldName] = match.Groups[step.FieldName].Value;
                    }
                }
                else
                {
                    // Otherwise, run a sub-regex on the match value
                    var subMatch = Regex.Match(match.Value, step.Selector);
                    if (subMatch.Success)
                    {
                        // Use group 1 if available, otherwise whole match
                        var val = subMatch.Groups.Count > 1 ? subMatch.Groups[1].Value : subMatch.Value;
                        result.Data.Fields[step.FieldName] = val;
                    }
                }
            }

            yield return result;
        }
    }
}