using Tendril.Core.Domain.Enums;

namespace Tendril.Core.Domain.Entities;

public enum SelectorReturnMode
{
    First = 0,
    All = 1
}
public class ScraperSelector
{
    public Guid Id { get; set; }

    public Guid ScraperDefinitionId { get; set; }
    public ScraperDefinition ScraperDefinition { get; set; } = null!;

    public string FieldName { get; set; } = null!;
    // Examples: "EventTitle", "Date", "StartTime", "ImageUrl"

    public SelectorType SelectorType { get; set; } // Css, XPath, Regex
    public string Selector { get; set; } = null!;

    public bool Outer { get; set; }

    public SelectorReturnMode ReturnMode { get; set; } = SelectorReturnMode.First;
}
