using Tendril.Core.Domain.Enums;

namespace Tendril.Api.Dtos;

public record EventRevisionDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public string? EventTitle { get; init; }
    public Guid? RawEventId { get; init; }
    public DateTimeOffset ChangedAtUtc { get; init; }
    public EventRevisionReason Reason { get; init; }
    public string? ChangedFieldsJson { get; init; }
}
