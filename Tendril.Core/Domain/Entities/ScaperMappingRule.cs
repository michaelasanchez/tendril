using System;
using System.Collections.Generic;
using System.Text;
using Tendril.Core.Domain.Enums;

namespace Tendril.Core.Domain.Entities;

public class ScraperMappingRule
{
    public Guid Id { get; set; }

    public Guid ScraperDefinitionId { get; set; }
    public ScraperDefinition ScraperDefinition { get; set; } = null!;

    // Raw field input (from selectors)
    public string SourceField { get; set; } = null!;

    // Final normalized Event property name
    public string TargetField { get; set; } = null!;
    // Examples: "Title", "StartDate", "StartTime", "Category", "Url"

    public string? CombineWithField { get; set; }

    public TransformType TransformType { get; set; }
    public string? TransformArgsJson { get; set; } // parameters for transforms
}
