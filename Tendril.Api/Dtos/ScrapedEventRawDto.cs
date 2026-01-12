namespace Tendril.Api.Dtos;

public class ScrapedEventRawDto
{
    public Guid Id { get; set; }
    public Guid? ScraperDefinitionId { get; set; }
    public Guid? ScraperAttemptHistoryId { get; set; }
    public Guid? EventId { get; set; }

    public DateTimeOffset ScrapedAtUtc { get; set; }

    public string RawDataJson { get; set; } = null!;
}
