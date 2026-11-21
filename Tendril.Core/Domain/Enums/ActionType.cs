namespace Tendril.Core.Domain.Enums;

public enum ActionType
{
    Synchronize,
    ConstantValue,
    Container,

    /* Element */
    Text,
    Attribute,

    /* Interaction */
    Click,
    Hover,
    Scroll,
    Input,
    //Page,

    /* Links */
    CaptureLink,
    FollowLink,
    CallApi
}
