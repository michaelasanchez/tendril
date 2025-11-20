using System;
using System.Collections.Generic;
using System.Text;

namespace Tendril.Core.Domain.Entities;

public class ScrapedEventRaw
{
    public Guid Id { get; set; }

    public Guid ScraperDefinitionId { get; set; }
    public ScraperDefinition ScraperDefinition { get; set; } = null!;

    public DateTimeOffset ScrapedAtUtc { get; set; }

    // JSON blob of all raw fields + values
    public string RawDataJson { get; set; } = null!;
}
