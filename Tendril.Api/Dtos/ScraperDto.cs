using Tendril.Core.Domain.Enums;

namespace Tendril.Api.Dtos;

public record ScraperDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string BaseUrl { get; set; }
    public bool Disabled { get; set; }
    public string Notes { get; set; }
    public ExecutionMode ExecutionMode { get; set; }
    public ExtractionStrategy ExtractionStrategy { get; set; }
    public PaginationType PaginationType { get; set; }
    public bool UseYearTracking { get; set; }
    public string State { get; set; }
    public string? LastSuccessUtc { get; set; }
    public string? LastFailureUtc { get; set; }
    public Guid? VenueId { get; set; }
    public List<ParentScraperDto>? Parents { get; set; }
}