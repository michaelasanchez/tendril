using System;
using System.Collections.Generic;
using System.Text;

namespace Tendril.Core.Domain.Entities;

public class ScraperAttemptHistory
{
    public Guid Id { get; set; }

    public Guid ScraperDefinitionId { get; set; }
    public ScraperDefinition ScraperDefinition { get; set; } = null!;

    public DateTimeOffset StartTimeUtc { get; set; }
    public DateTimeOffset EndTimeUtc { get; set; }

    public bool Success { get; set; }
    public int ExtractedCount { get; set; }
    public string? ErrorMessage { get; set; }
}
