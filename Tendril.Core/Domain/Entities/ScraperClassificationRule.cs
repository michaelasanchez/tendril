using Tendril.Core.Domain.Enums;

namespace Tendril.Core.Domain.Entities;

public class ScraperClassificationRule
{
    public Guid Id { get; set; }
    public Guid ScraperDefinitionId { get; set; }
    public ScraperDefinition ScraperDefinition { get; set; } = null!;
    public int Order { get; set; }
    public bool Disabled { get; set; } = false;
    public string SourceJsonPath { get; set; } = null!;
    public ConditionType ConditionType { get; set; }
    public string ConditionValue { get; set; } = null!;
    public ICollection<RuleAssignment> Assignments { get; set; } = [];
}
