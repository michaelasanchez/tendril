namespace Tendril.Api.Dtos;

public class CreateScraperRequest
{
    public string Name { get; set; } = null!;
    public string BaseUrl { get; set; } = null!;
    public bool IsDynamic { get; set; }
    public Guid? VenueId { get; set; }
}
