namespace Tendril.Core.Domain.Entities;

public class ScraperAttemptHistory
{
    public Guid Id { get; set; }

    public Guid ScraperDefinitionId { get; set; }
    public ScraperDefinition ScraperDefinition { get; set; } = null!;

    public ICollection<ScrapedEventRaw> ScrapedEventRaws { get; set; } = [];
    public ICollection<EventRevision> Revisions { get; set; } = [];

    public DateTimeOffset StartTimeUtc { get; set; }
    public DateTimeOffset EndTimeUtc { get; set; }

    public bool Success { get; set; }
    public int Extracted { get; set; }
    public int Mapped { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Errored { get; set; }

    public string? ErrorMessage { get; set; }
}
