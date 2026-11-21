namespace Tendril.Core.Domain.Enums;

public enum SynchronizationCondition
{
    Delay,
    SelectorAttached,
    SelectorVisible,
    SelectorHidden,
    DomContentLoaded,
    Load,
    NetworkIdle,
    JsonLdPresent
}