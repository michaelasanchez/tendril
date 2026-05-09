using Tendril.Core.Domain.Enums;

namespace Tendril.Core.Domain.Entities;

public class Event
{
    public Guid Id { get; set; }

    public Guid ScraperDefinitionId { get; set; }
    public ScraperDefinition Scraper { get; set; } = null!;

    public ICollection<EventRevision> Revisions { get; set; } = [];

    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; } = null!;

    public ICollection<EventTag> EventTags { get; set; } = [];

    public Guid? VenueId { get; set; }
    public Venue? Venue { get; set; } = null;

    public string Title { get; set; } = null!;
    public string? Location { get; set; }
    public string? Description { get; set; }

    public DateTimeOffset StartUtc { get; set; }
    public DatePrecision StartPrecision { get; set; }
    public DateTimeOffset? EndUtc { get; set; }
    public DatePrecision? EndPrecision { get; set; }

    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    public string? DetailsUrl { get; set; }
    public string? ImageUrl { get; set; }
    public string? TicketUrl { get; set; }

    public EventStatus Status { get; set; }
    public bool RequiresReview { get; set; }

    public DateTimeOffset ScrapedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public DateTimeOffset? StatusAtUtc { get; set; }
    public DateTimeOffset? RequiresReviewAtUtc { get; set; }
}
