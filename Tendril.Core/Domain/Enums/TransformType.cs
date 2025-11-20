using System;
using System.Collections.Generic;
using System.Text;

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
    ToLower,
    ToUpper
}
