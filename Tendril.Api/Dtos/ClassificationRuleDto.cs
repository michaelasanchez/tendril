using Tendril.Core.Domain.Enums;

namespace Tendril.Api.Dtos;

public record ClassificationRuleDto(
    Guid Id,
    Guid ScraperDefinitionId,
    int Order,
    bool Disabled,
    string SourceJsonPath,
    ConditionType ConditionType,
    string ConditionValue,
    List<RuleAssignmentDto> Assignments
);

public class CreateClassificationRuleRequest
{
    public Guid ScraperDefinitionId { get; set; }
    public int Order { get; set; }
    public bool Disabled { get; set; } = false;
    public string SourceJsonPath { get; set; } = null!;
    public ConditionType ConditionType { get; set; }
    public string ConditionValue { get; set; } = null!;
    public List<CreateRuleAssignment> Assignments { get; set; } = [];
}

public class UpdateClassificationRuleRequest
{
    public Guid? ScraperDefinitionId { get; set; }
    public int? Order { get; set; }
    public bool? Disabled { get; set; } = false;
    public string? SourceJsonPath { get; set; } = null!;
    public ConditionType? ConditionType { get; set; }
    public string? ConditionValue { get; set; } = null!;
    public List<UpdateRuleAssignment>? Assignments { get; set; } = [];
}
