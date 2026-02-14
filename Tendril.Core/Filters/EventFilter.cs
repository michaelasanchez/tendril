namespace Tendril.Core.Filters;

public class EventFilter
{
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public string? Title { get; set; }
    public List<string>? Categories { get; set; }
    public List<Guid>? VenueIds { get; set; }
}
