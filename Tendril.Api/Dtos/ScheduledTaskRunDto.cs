namespace Tendril.Api.Dtos;

public record ScheduledTaskRunDto(
    Guid Id,
    Guid ScheduledTaskId,
    string ScheduledTaskName,
    string Status,
    DateTimeOffset StartTimeUtc,
    DateTimeOffset? EndTimeUtc,
    int RetryCount,
    int AttemptCount,
    int SuccessfulAttemptCount,
    int FailedAttemptCount,
    int InProgressAttemptCount,
    IReadOnlyList<ScheduledTaskRunAttemptDto> Attempts);

public record ScheduledTaskRunAttemptDto(
    Guid Id,
    Guid ScraperDefinitionId,
    string ScraperName,
    string Status,
    DateTimeOffset StartTimeUtc,
    DateTimeOffset? EndTimeUtc,
    int Extracted,
    int Mapped,
    int Created,
    int Updated,
    int Skipped,
    int Errored,
    string? ErrorMessage);