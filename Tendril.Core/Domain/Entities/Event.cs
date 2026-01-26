using Tendril.Core.Domain.Enums;

namespace Tendril.Core.Domain.Entities;

public class Event
{
    public Guid Id { get; set; }

    public Guid ScraperDefinitionId { get; set; }
    public ScraperDefinition Scraper { get; set; } = null!;

    public ICollection<ScrapedEventRaw> ScrapedEventRaws { get; set; } = [];

    public Guid? VenueId { get; set; }
    public Venue? Venue { get; set; }

    public string Title { get; set; } = null!;
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }

    public DateTimeOffset StartUtc { get; set; }
    public DateTimeOffset? EndUtc { get; set; }

    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    public string? DetailsUrl { get; set; }
    public string? ImageUrl { get; set; }
    public string? TicketUrl { get; set; }

    public EventStatus Status { get; set; }

    public DateTimeOffset ScrapedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public DateTimeOffset? StatusAtUtc { get; set; }
}
