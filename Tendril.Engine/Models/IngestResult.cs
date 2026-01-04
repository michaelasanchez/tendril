using Tendril.Core.Domain.Entities;

namespace Tendril.Engine.Models;


public class IngestResult
{
    public ScraperAttemptHistory? Attempt { get; set; }
    public bool Success { get; set; }
    public List<string>? Errors { get; set; }

    public List<ScrapedEventRaw>? Raw { get; set; }
    public List<Event>? Mapped { get; set; }
    public List<string>? MappingSummary { get; set; }
}
