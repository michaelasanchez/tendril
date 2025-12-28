using Tendril.Core.Domain.Enums;

namespace Tendril.Api.Dtos;

public record ScraperDto(
    Guid Id,
    string Name,
    string BaseUrl,
    PaginationType paginationType,
    string State,
    string? LastSuccessUtc,
    string? LastFailureUtc,
    Guid? venueId
);