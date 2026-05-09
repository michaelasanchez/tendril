namespace Tendril.Core.Domain.Enums;

public enum TransformType
{
    None,
    Constant,
    Trim,
    RegexExtract,
    RegexReplace,
    Split,
    Combine,
    ParseDate,
    ParseTime,
    ParseExact,
    ToLower,
    ToUpper,
    Currency,
    DecodeHtml,
    StripHtml,
    SrcSetToUrl
}
