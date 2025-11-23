using Tendril.Core.Domain.Enums;

namespace Tendril.Api.Dtos;

public record MappingRuleDto(
    Guid Id,
    string TargetField,
    string SourceField,
    string? CombineWithField,
    TransformType TransformType
);

public class CreateMappingRuleRequest
{
    public string TargetField { get; set; } = null!;
    public string SourceField { get; set; } = null!;
    public string? CombineWithField { get; set; }
    public TransformType TransformType { get; set; } = TransformType.None;
}

public class UpdateMappingRuleRequest
{
    public string? TargetField { get; set; }
    public string? SourceField { get; set; }
    public string? CombineWithField { get; set; }
    public TransformType? TransformType { get; set; }
}
