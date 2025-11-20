using Tendril.Core.Domain.Enums;

namespace Tendril.Core.Domain.Entities;

public class ScraperSelector
{
    public Guid Id { get; set; }

    public Guid ScraperDefinitionId { get; set; }
    public ScraperDefinition ScraperDefinition { get; set; } = null!;

    public string FieldName { get; set; } = null!;
    // Examples: "EventTitle", "Date", "StartTime", "ImageUrl"

    public SelectorType SelectorType { get; set; } // Css, XPath, Regex
    public string Selector { get; set; } = null!;

    public bool Multiple { get; set; } // e.g., multiple performers
}
