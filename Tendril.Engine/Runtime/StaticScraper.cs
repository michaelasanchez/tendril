using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore; // Still needed for CSS
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Engine.Abstractions;
using Tendril.Engine.Models;

namespace Tendril.Engine.Runtime;

public class StaticScraper(IJsonLdProcessor jsonLd) // Removed Factory injection for clarity, assume string passed in
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
            if (data != null) yield return new ScrapeYieldItem { Data = data };
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
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // A. Find Containers
        var containerDef = def.Selectors.SingleOrDefault(x => x.Type == SelectorType.Container);
        if (containerDef == null) yield break;

        IEnumerable<HtmlNode>? nodes = null;

        if (def.ExtractionStrategy == ExtractionStrategy.XPath)
        {
            // Native HAP method
            nodes = doc.DocumentNode.SelectNodes(containerDef.Selector);
        }
        else // Strategy == Css
        {
            // Extension method
            nodes = doc.DocumentNode.QuerySelectorAll(containerDef.Selector);
        }

        if (nodes == null) yield break;

        foreach (var node in nodes)
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
                    var targetNode = FindNode(node, step, def.ExtractionStrategy);
                    var url = targetNode?.GetAttributeValue("href", "");

                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        result = result with { ChildUrl = url, ChildScraperId = step.ChildScraperDefinitionId };
                    }
                }
                // FIELD LOGIC
                else
                {
                    ExtractField(node, step, result.Data, doc, def.ExtractionStrategy);
                }
            }

            yield return result;
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

    private void ExtractField(HtmlNode parentNode, ScraperSelector step, RawScrapedEvent rawEvent, HtmlDocument doc, ExtractionStrategy strategy)
    {
        try
        {
            HtmlNode? targetNode = null;

            if (step.Root)
            {
                targetNode = strategy == ExtractionStrategy.XPath
                    ? doc.DocumentNode.SelectSingleNode(step.Selector)
                    : doc.DocumentNode.QuerySelector(step.Selector);
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
        catch { /* Ignore */ }
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