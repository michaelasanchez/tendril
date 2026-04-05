namespace Tendril.Core.Domain.Enums;

public enum ActionType
{
    ConstantValue,
    Container,

    Text,
    Attribute,

    Click,
    Hover,
    Scroll,
    Input,
    //Page,

    CaptureLink,
    FollowLink,
    CallApi
}
