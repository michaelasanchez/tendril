using Tendril.Engine.Models;

namespace Tendril.Engine.Extensions;

public static class ScraperExtensions
{
    public static RawScrapedData MergeData(this RawScrapedData parent, RawScrapedData child)
    {
        var merged = new RawScrapedData();

        foreach (var kvp in parent.Fields) merged.Fields[kvp.Key] = kvp.Value;
        foreach (var kvp in child.Fields)
        {
            if (!string.IsNullOrWhiteSpace(kvp.Value))
            {
                merged.Fields[kvp.Key] = kvp.Value;
            }
        }

        return merged;
    }

    public static ScrapeYieldItem Merge(this ScrapeYieldItem parent, ScrapeYieldItem child)
    {
        return new ScrapeYieldItem
        {
            Data = parent.Data.MergeData(child.Data),

            // Keep the child's navigation details since that is the "current" context
            ChildUrl = child.ChildUrl,
            ChildScraperId = child.ChildScraperId
        };
    }
}
