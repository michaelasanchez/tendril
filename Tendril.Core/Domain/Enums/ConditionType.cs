namespace Tendril.Core.Domain.Enums;

public enum ConditionType
{
    Default = 0,
    Equals = 1,
    NotEquals = 2,
    Contains = 3,
    NotContains = 4,
    StartsWith = 5,
    EndsWith = 6,
    GreaterThan = 7,
    LessThan = 8,
    GreaterThanOrEqualTo = 9,
    LessThanOrEqualTo = 10,
    RegexMatch = 11,
    RegexNotMatch = 12
}
