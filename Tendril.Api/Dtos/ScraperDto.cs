using Tendril.Core.Domain.Enums;

namespace Tendril.Api.Dtos;

public record ScraperDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string BaseUrl { get; set; }
    public bool IsEventFeed { get; set; }
    public bool Disabled { get; set; }
    public string Notes { get; set; }
    public bool HasSuggestions { get; set; }
    public bool RequiresReview { get; set; }
    public ExecutionMode ExecutionMode { get; set; }
    public ExtractionStrategy ExtractionStrategy { get; set; }
    public PaginationType PaginationType { get; set; }
    public bool UseYearTracking { get; set; }
    public bool UseHeadlessBrowser { get; set; }
    public string State { get; set; }
    public string? LastSuccessUtc { get; set; }
    public string? LastFailureUtc { get; set; }
    public Guid? VenueId { get; set; }
    public Core.Domain.Enums.HttpMethod? Method { get; set; }
    public List<ApiParameterDto>? Parameters { get; set; }
    public List<ParentScraperDto>? Parents { get; set; }
}