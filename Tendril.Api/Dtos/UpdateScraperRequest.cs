using Tendril.Core.Domain.Enums;

namespace Tendril.Api.Dtos;

public class UpdateScraperRequest
{
    public string? Name { get; set; }
    public string? BaseUrl { get; set; }
    public bool? Disabled { get; set; }
    public string? Notes { get; set; }
    public ExecutionMode? ExecutionMode { get; set; }
    public ExtractionStrategy? ExtractionStrategy { get; set; }
    public PaginationType? PaginationType { get; set; }
    public Guid? VenueId { get; set; }
}