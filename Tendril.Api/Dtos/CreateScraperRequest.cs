using Tendril.Core.Domain.Enums;

namespace Tendril.Api.Dtos;

public class CreateScraperRequest
{
    public string Name { get; set; } = null!;
    public string BaseUrl { get; set; } = null!;
    public bool IsEventFeed { get; set; }
    public bool Disabled { get; set; } = false;
    public string Notes { get; set; } = null!;
    public ExecutionMode? ExecutionMode { get; set; }
    public ExtractionStrategy? ExtractionStrategy { get; set; }
    public PaginationType? PaginationType { get; set; }
    public bool UseYearTracking { get; set; } = true;
    public Guid? VenueId { get; set; }
    public Core.Domain.Enums.HttpMethod? Method { get; set; }
    public List<CreateApiParameterRequest>? Parameters { get; set; }
}
