using Tendril.Core.Domain.Enums;

namespace Tendril.Core.Domain.Entities;

public class ScraperAttemptLogEntry
{
    public Guid Id { get; set; }

    public Guid AttemptId { get; set; }
    public ScraperAttemptHistory Attempt { get; set; } = null!;

    public DateTimeOffset TimestampUtc { get; set; }
    public ScraperAttemptLogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
}