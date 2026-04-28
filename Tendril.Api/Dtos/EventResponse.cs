using Tendril.Core.Domain;

namespace Tendril.Api.Dtos;

public class EventResponse : PagedResponse<EventDto>
{
    public List<Guid> CategoryIds { get; set; } = [];
    public List<Guid> VenueIds { get; set; } = [];
}
