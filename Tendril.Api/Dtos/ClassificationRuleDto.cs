using Tendril.Core.Domain.Enums;

namespace Tendril.Api.Dtos;

public record ClassificationRuleDto(
    Guid Id,
    Guid ScraperDefinitionId,
    int Order,
    bool Disabled,
    string SourceJsonPath,
    ConditionType ConditionType,
    string ConditionValue
);

public class CreateClassificationRuleRequest
{
    public Guid ScraperDefinitionId { get; set; }
    public int Order { get; set; }
    public bool Disabled { get; set; } = false;
    public string SourceJsonPath { get; set; } = null!;
    public ConditionType ConditionType { get; set; }
    public string ConditionValue { get; set; } = null!;
}

public class UpdateClassificationRuleRequest
{
    public Guid? ScraperDefinitionId { get; set; }
    public int? Order { get; set; }
    public bool? Disabled { get; set; } = false;
    public string? SourceJsonPath { get; set; } = null!;
    public ConditionType? ConditionType { get; set; }
    public string? ConditionValue { get; set; } = null!;
}
