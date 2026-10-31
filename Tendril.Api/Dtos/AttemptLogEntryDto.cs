using Tendril.Core.Domain.Enums;

namespace Tendril.Api.Dtos;

public class AttemptLogEntryDto
{
    public Guid Id { get; set; }

    public DateTimeOffset TimestampUtc { get; set; }
    public ScraperAttemptLogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
}
