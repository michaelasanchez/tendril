namespace Tendril.Api.Dtos;

public record ScraperActionDto(
    Guid Id,
    string Name,
    string FieldName,
    string Selector,
    int Order,
    bool Root,
    string Type,
    string? Attribute,
    int? Delay,
    string? ConstantValue,
    string? InteractionValue,
    Guid? ChildScraperId,
    bool IgnoreDuplicateUrls,
    bool IsPaginationTrigger,
    bool Disabled
);
