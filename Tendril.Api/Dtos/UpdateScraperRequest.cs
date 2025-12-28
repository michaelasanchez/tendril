using Tendril.Core.Domain.Enums;

namespace Tendril.Api.Dtos;

public class UpdateScraperRequest
{
    public string? Name { get; set; }
    public string? BaseUrl { get; set; }
    public PaginationType? PaginationType { get; set; }
    public Guid? VenueId { get; set; }
}