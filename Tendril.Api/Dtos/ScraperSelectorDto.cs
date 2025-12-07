namespace Tendril.Api.Dtos;

public record ScraperSelectorDto(
    Guid Id,
    string FieldName,
    string Selector,
    int Order,
    bool Root,
    string Type
);
