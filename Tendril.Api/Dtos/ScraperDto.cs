using Tendril.Core.Domain.Enums;

namespace Tendril.Api.Dtos;

public record ScraperDto(
    Guid Id,
    string Name,
    string BaseUrl,
    bool Disabled,
    string Notes,
    ExecutionMode ExecutionMode,
    ExtractionStrategy ExtractionStrategy,
    PaginationType PaginationType,
    bool UseReferenceYear,
    string State,
    string? LastSuccessUtc,
    string? LastFailureUtc,
    Guid? VenueId
);