using Tendril.Api.Dtos;

public class PendingEventReviewDto
{
    public Guid ScraperId { get; set; }

    // The incoming scraped event currently awaiting approval
    public EventDto PendingEvent { get; set; }

    // Existing live events that look suspiciously similar
    public IEnumerable<EventDto> PotentialMatches { get; set; }
}