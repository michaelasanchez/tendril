namespace Tendril.Api.Dtos;

public record VenueDto(
    Guid Id,
    string Name,
    string? Address,
    string? Website
);