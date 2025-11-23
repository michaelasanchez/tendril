namespace Tendril.Api.Dtos;

public record ScraperSelectorDto(
    Guid Id,
    string FieldName,
    string Selector,
    string SelectorType,
    bool Outer
);
