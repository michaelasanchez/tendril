using Tendril.Core.Domain.Enums;

namespace Tendril.Core.Domain.Entities;

public class ScraperMappingRule
{
    public Guid Id { get; set; }

    public Guid ScraperDefinitionId { get; set; }
    public ScraperDefinition ScraperDefinition { get; set; } = null!;

    public string SourceField { get; set; } = null!;

    public string TargetField { get; set; } = null!;

    public string? CombineWithField { get; set; }

    public int Order { get; set; } = 0;
    public TransformType TransformType { get; set; }

    public string? Format { get; set; }
    public string? RegexPattern { get; set; }
    public string? RegexReplacement { get; set; }
    public string? SplitDelimiter { get; set; }
}
