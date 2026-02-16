namespace Tendril.Core.Domain.Enums;

public enum ConditionType
{
    Default,
    Equals,
    NotEquals,
    Contains,
    NotContains,
    StartsWith,
    EndsWith,
    GreaterThan,
    LessThan,
    GreaterThanOrEqualTo,
    LessThanOrEqualTo,
    RegexMatch,
    RegexNotMatch
}
