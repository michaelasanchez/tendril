namespace Tendril.Engine.Models;

public record UpdateResult(
    bool Updated,
    string Field,
    string? OldValue,
    string? NewValue
);