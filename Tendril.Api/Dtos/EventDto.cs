namespace Tendril.Api.Dtos;

public record EventDto(
    Guid Id,
    string Title,
    string? Description,
    DateTimeOffset StartUtc,
    DateTimeOffset? EndUtc,
    string? TicketUrl,
    string? Category,
    string? ImageUrl,
    string VenueName
);