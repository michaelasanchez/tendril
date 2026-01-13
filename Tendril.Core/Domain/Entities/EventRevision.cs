using Tendril.Core.Domain.Enums;

namespace Tendril.Core.Domain.Entities;

public class EventRevision
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }
    public Event? Event { get; set; }

    public Guid AttemptHistoryId { get; set; }
    public ScraperAttemptHistory? AttemptHistory { get; set; }

    public Guid RawEventId { get; set; }
    public ScrapedEventRaw? RawEvent { get; set; }

    public EventRevisionReason Reason { get; set; }

    public DateTimeOffset ChangedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public string? ChangedFieldsJson { get; set; }
}
