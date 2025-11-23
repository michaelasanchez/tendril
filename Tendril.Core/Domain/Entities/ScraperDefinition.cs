using System;
using Tendril.Core.Domain.Enums;

namespace Tendril.Core.Domain.Entities;

public class ScraperDefinition
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;
    public string BaseUrl { get; set; } = null!;

    // Dynamic vs Static scrapers
    public bool IsDynamic { get; set; }

    // Cron or interval string
    public string Schedule { get; set; } = "0 */6 * * *"; // every 6 hours


    // Health monitoring
    public ScraperState State { get; set; } = ScraperState.Unknown;
    public DateTimeOffset? LastSuccessUtc { get; set; }
    public DateTimeOffset? LastFailureUtc { get; set; }
    public string? LastErrorMessage { get; set; }

    // Optional: tie scraper to a venue
    public Guid? VenueId { get; set; }
    public Venue? Venue { get; set; }

    public List<ScraperSelector> Selectors { get; set; } = new();
    public List<ScraperMappingRule> MappingRules { get; set; } = new();
    public List<ScraperAttemptHistory> AttemptHistory { get; set; } = new();
}
