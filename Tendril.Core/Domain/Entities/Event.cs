using System;
using System.Collections.Generic;
using System.Text;

namespace Tendril.Core.Domain.Entities;

public class Event
{
    public Guid Id { get; set; }

    public Guid ScraperDefinitionId { get; set; }
    public ScraperDefinition Scraper { get; set; } = null!;

    public Guid? VenueId { get; set; }
    public Venue? Venue { get; set; }

    public string Title { get; set; } = null!;
    public string? Description { get; set; }

    public DateTimeOffset StartUtc { get; set; }
    public DateTimeOffset? EndUtc { get; set; }

    public string? TicketUrl { get; set; }
    public string? Category { get; set; }

    public string? ImageUrl { get; set; }
}
