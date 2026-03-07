using Tendril.Core.Domain.Enums;

namespace Tendril.Core.Domain.Entities;

public class ScraperDefinition
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;
    public string BaseUrl { get; set; } = null!;

    public string Notes { get; set; } = "";
    public bool RequireReview { get; set; } = false;
    public bool Disabled { get; set; } = false;

    public ExecutionMode ExecutionMode { get; set; } = ExecutionMode.Dynamic;
    public ExtractionStrategy ExtractionStrategy { get; set; } = ExtractionStrategy.Css;
    public PaginationType PaginationType { get; set; } = PaginationType.None;
    public bool UseReferenceYear { get; set; } = false;

    // Health monitoring
    public ScraperState State { get; set; } = ScraperState.Unknown;
    public DateTimeOffset? LastSuccessUtc { get; set; }
    public DateTimeOffset? LastFailureUtc { get; set; }
    public string? LastErrorMessage { get; set; }


    // Optional: tie scraper to a venue
    public Guid? VenueId { get; set; }
    public Venue? Venue { get; set; }

    public List<ScraperSelector> Selectors { get; set; } = [];
    public List<ScraperMappingRule> MappingRules { get; set; } = [];
    public List<ScraperClassificationRule> ClassificationRules { get; set; } = [];
    public List<ScraperAttemptHistory> AttemptHistory { get; set; } = [];
}
