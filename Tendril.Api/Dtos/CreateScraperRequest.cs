using Tendril.Core.Domain.Enums;

namespace Tendril.Api.Dtos;

public class CreateScraperRequest
{
    public string Name { get; set; } = null!;
    public string BaseUrl { get; set; } = null!;
    public PaginationType? PaginationType { get; set; }
    public Guid? VenueId { get; set; }
}
