namespace Tendril.Data.Models;

public class EventFilter
{
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public string? Title { get; set; }
    public List<Guid>? CategoryIds { get; set; }
    public List<Guid>? VenueIds { get; set; }
}
