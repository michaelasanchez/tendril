namespace Tendril.Api.Dtos;

public record ScraperDto(
    Guid Id,
    string Name,
    string BaseUrl,
    bool IsDynamic,
    string State,
    string? LastSuccessUtc,
    string? LastFailureUtc,
    Guid? venueId
);