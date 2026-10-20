namespace Tendril.Api.Dtos;

public record PatchEventsRequest
{
    public List<Guid> Ids { get; set; } = [];
    public PatchEventRequest Patch { get; set; } = new();
};