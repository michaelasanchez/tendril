using Tendril.Core.Domain.Enums;

namespace Tendril.Api.Dtos;

public class CreateActionRequest
{
    public string Name { get; set; } = null!;
    public string FieldName { get; set; } = null!;
    public string Selector { get; set; } = null!;
    public int Order { get; set; }
    public bool Root { get; set; }
    public ActionType Type { get; set; }
    public string? Attribute { get; set; }
    public int? Delay { get; set; }
    public string? ConstantValue { get; set; }
    public string? InteractionValue { get; set; }
    public Guid? ChildScraperId { get; set; }
    public bool IgnoreDuplicateUrls { get; set; } = true;
    public bool IsPaginationTrigger { get; set; }
    public bool Disabled { get; set; }
}
