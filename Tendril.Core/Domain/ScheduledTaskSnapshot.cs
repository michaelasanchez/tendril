using Tendril.Core.Domain.Enums;

namespace Tendril.Core.Domain;

/// <summary>
/// Read-only copy of the task settings the worker needs. The worker never tracks a
/// ScheduledTask entity, so edits made in the admin UI mid-run are never overwritten.
/// </summary>
public sealed record ScheduledTaskSnapshot(
    Guid Id,
    string Name,
    bool IsDisabled,
    string CronExpression,
    SelectionStrategy SelectionStrategy,
    int MaxRetries);
