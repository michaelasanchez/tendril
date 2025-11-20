using System;
using System.Collections.Generic;
using System.Text;

namespace Tendril.Core.Domain.Entities;

public class Venue
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public string? Website { get; set; }

    public List<Event> Events { get; set; } = new();
    public List<ScraperDefinition> Scrapers { get; set; } = new();
}
