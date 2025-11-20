using Tendril.Core.Domain.Enums;

namespace Tendril.Api.Dtos;

public class CreateSelectorRequest
{
    public string FieldName { get; set; } = null!;
    public string Selector { get; set; } = null!;
    public SelectorType SelectorType { get; set; }
}
