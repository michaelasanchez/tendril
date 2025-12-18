namespace Tendril.Core.Domain.Entities;

public class ScrapedEventRaw
{
    public Guid Id { get; set; }

    public Guid? ScraperDefinitionId { get; set; }
    public ScraperDefinition? ScraperDefinition { get; set; } = null!;

    public Guid? ScraperAttemptHistoryId { get; set; }
    public ScraperAttemptHistory? ScraperAttemptHistory { get; set; } = null!;

    public Guid? EventId { get; set; }
    public Event? Event { get; set; }

    public DateTimeOffset ScrapedAtUtc { get; set; }

    // JSON blob of all raw fields + values
    public string RawDataJson { get; set; } = null!;
}
