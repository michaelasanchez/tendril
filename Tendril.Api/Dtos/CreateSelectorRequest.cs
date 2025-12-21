using Tendril.Core.Domain.Enums;

namespace Tendril.Api.Dtos;

public class CreateSelectorRequest
{
    public string FieldName { get; set; } = null!;
    public string Selector { get; set; } = null!;
    public int Order { get; set; }
    public bool Root { get; set; }
    public SelectorType Type { get; set; }
    public string? Attribute { get; set; }
    public int? Delay { get; set; }
}
