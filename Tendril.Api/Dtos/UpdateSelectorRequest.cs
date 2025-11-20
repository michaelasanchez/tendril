using Tendril.Core.Domain.Enums;

namespace Tendril.Api.Dtos;

public class UpdateSelectorRequest
{
    public string? FieldName { get; set; }
    public string? Selector { get; set; }
    public SelectorType? SelectorType { get; set; }
}
