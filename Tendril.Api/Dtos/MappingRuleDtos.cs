using Tendril.Core.Domain.Enums;

namespace Tendril.Api.Dtos;

public record MappingRuleDto(
    Guid Id,
    string TargetField,
    string SourceField,
    string? CombineWithField,
    int Order,
    TransformType TransformType,
    string? Format = null,
    string? RegexPattern = null,
    string? RegexReplacement = null,
    string? SplitDelimiter = null
);

public class CreateMappingRuleRequest
{
    public string TargetField { get; set; } = null!;
    public string SourceField { get; set; } = null!;
    public string? CombineWithField { get; set; }
    public int Order { get; set; } = 0;
    public TransformType TransformType { get; set; } = TransformType.None;
    public string? Format { get; set; }
    public string? RegexPattern { get; set; }
    public string? RegexReplacement { get; set; }
    public string? SplitDelimiter { get; set; }
}

public class UpdateMappingRuleRequest
{
    public string? TargetField { get; set; }
    public string? SourceField { get; set; }
    public string? CombineWithField { get; set; }
    public int? Order { get; set; }
    public TransformType? TransformType { get; set; }
    public string? Format { get; set; }
    public string? RegexPattern { get; set; }
    public string? RegexReplacement { get; set; }
    public string? SplitDelimiter { get; set; }
}
