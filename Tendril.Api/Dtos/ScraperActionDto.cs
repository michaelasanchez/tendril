using Tendril.Core.Domain.Enums;

namespace Tendril.Api.Dtos;

public record ScraperActionDto(
    Guid Id,
    string Name,
    string FieldName,
    int Order,
    bool Root,
    string Type,
    string Selector,
    string? Attribute,
    SynchronizationCondition? Synchronization,
    int? Delay,
    string? ConstantValue,
    string? InteractionValue,
    Guid? ChildScraperId,
    bool? IgnoreDuplicateUrls,
    bool IsPaginationTrigger,
    bool Disabled
);
