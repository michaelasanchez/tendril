namespace Tendril.Api.Dtos;

public record ScheduledTaskDto(
    Guid Id,
    string Name,
    string? Notes,
    bool IsDisabled,
    string CronExpression,
    DateTimeOffset NextRunAtUtc,
    string SelectionStrategy,
    string Status,
    DateTimeOffset? LastRunStartedAtUtc,
    DateTimeOffset? LastRunCompletedAtUtc,
    IEnumerable<Guid> ScraperIds);

public record CreateScheduledTaskRequest(
    string Name,
    string? Notes,
    bool IsDisabled,
    string CronExpression,
    string SelectionStrategy,
    List<Guid>? ScraperIds);

public record UpdateScheduledTaskRequest(
    string? Name,
    string? Notes,
    bool? IsDisabled,
    string? CronExpression,
    DateTimeOffset? NextRunAtUtc,
    string? SelectionStrategy,
    string? Status,
    List<Guid>? ScraperIds);