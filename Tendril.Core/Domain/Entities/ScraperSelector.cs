using Tendril.Core.Domain.Enums;

namespace Tendril.Core.Domain.Entities;

public class ScraperSelector
{
    public Guid Id { get; set; }

    public Guid ScraperDefinitionId { get; set; }
    public ScraperDefinition ScraperDefinition { get; set; } = null!;

    public string FieldName { get; set; } = null!;
    public string Selector { get; set; } = null!;

    public int Order { get; set; } = 0;
    public bool Root { get; set; } = false;
    public SelectorType Type { get; set; } = SelectorType.Text;

    //public string? Attribute { get; set; }
    //public int? WaitAfterMs { get; set; }
}
