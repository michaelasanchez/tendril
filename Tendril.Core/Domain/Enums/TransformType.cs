namespace Tendril.Core.Domain.Enums;

public enum TransformType
{
    None,
    Trim,
    RegexExtract,
    RegexReplace,
    Split,
    Combine,
    ParseDate,
    ParseTime,
    ParseExact,
    ParseLoose,
    ToLower,
    ToUpper,
    Currency,
    SrcSetToUrl,
}
