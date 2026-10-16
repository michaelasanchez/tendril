using Microsoft.Playwright;
using System.Collections.Concurrent;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;

namespace Tendril.Engine.Models;

public class ScrapeContext
{
    public IBrowserContext? BrowserContext { get; set; }
    public HttpClient? StaticClient { get; set; }

    public ScrapeYieldItem? ParentItem { get; set; }
    public List<ScraperAttemptLogEntry> LogEntries { get; } = [];

    public void AddLog(ScraperAttemptLogLevel level, string message)
    {
        LogEntries.Add(new ScraperAttemptLogEntry
        {
            Id = Guid.NewGuid(),
            TimestampUtc = DateTimeOffset.UtcNow,
            Level = level,
            Message = message
        });
    }

    private readonly ConcurrentDictionary<string, byte> _visitedUrls = new();
    public bool HasVisited(string url) => _visitedUrls.ContainsKey(url);
    public void MarkVisited(string url) => _visitedUrls.TryAdd(url, 0);
}
