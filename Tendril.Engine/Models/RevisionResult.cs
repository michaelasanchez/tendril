namespace Tendril.Engine.Models;

public record RevisionResult(
    bool Updated,
    string Field,
    string? OldValue,
    string? NewValue
);