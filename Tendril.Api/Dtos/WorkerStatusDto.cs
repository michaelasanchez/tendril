namespace Tendril.Api.Dtos;

public record WorkerStatusDto(
    string Status,
    bool IsOnline,
    Guid? SessionId,
    string? MachineName,
    int? ProcessId,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? LastHeartbeatAtUtc,
    DateTimeOffset? StoppedAtUtc,
    int? HeartbeatAgeSeconds);