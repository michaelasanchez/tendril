namespace Tendril.Api.Dtos;

public class UpdateScraperRequest
{
    public string? Name { get; set; }
    public string? BaseUrl { get; set; }
    public bool? IsDynamic { get; set; }
    public Guid? VenueId { get; set; }
}