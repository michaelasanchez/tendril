using Tendril.Core.Domain.Enums;

namespace Tendril.Api.Dtos;

public class UpdateSelectorRequest
{
    public string? FieldName { get; set; }
    public string? Selector { get; set; }
    public int? Order { get; set; }
    public bool? Root { get; set; }
    public SelectorType? Type { get; set; }
}
