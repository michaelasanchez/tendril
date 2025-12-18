using Tendril.Core.Domain.Entities;

namespace Tendril.Engine.Models;


public class IngestResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public ScraperAttemptHistory? Attempt { get; set; }
    public List<ScrapedEventRaw>? Scraped { get; set; }
    public List<Event>? Mapped { get; set; }
}
