using Microsoft.Playwright;
using System.Collections.Concurrent;

namespace Tendril.Engine.Models;

public class ScrapeContext
{
    public IBrowserContext? DynamicBrowser { get; set; }
    public HttpClient? StaticClient { get; set; }

    public ScrapeYieldItem? ParentItem { get; set; }
    public bool ParentIgnoreDuplicateUrls { get; set; } = false;

    private readonly ConcurrentDictionary<string, byte> _visitedUrls = new();
    public bool HasVisited(string url) => _visitedUrls.ContainsKey(url);
    public void MarkVisited(string url) => _visitedUrls.TryAdd(url, 0);
}
