using Tendril.Api.Dtos;

public class PendingEventReviewDto
{
    // The incoming scraped event currently awaiting approval
    public EventDto PendingEvent { get; set; }

    // Existing live events that look suspiciously similar
    public IEnumerable<EventDto> PotentialMatches { get; set; }
}