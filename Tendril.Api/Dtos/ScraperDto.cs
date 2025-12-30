using Tendril.Core.Domain.Enums;

namespace Tendril.Api.Dtos;

public record ScraperDto(
    Guid Id,
    string Name,
    string BaseUrl,
    ExecutionMode ExecutionMode,
    ExtractionStrategy ExtractionStrategy,
    PaginationType PaginationType,
    string State,
    string? LastSuccessUtc,
    string? LastFailureUtc,
    Guid? VenueId
);