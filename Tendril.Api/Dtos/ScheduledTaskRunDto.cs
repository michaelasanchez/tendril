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
    int InProgressAttemptCount);