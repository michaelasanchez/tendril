namespace Tendril.Api.Dtos;

public record ScheduledTaskDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Notes { get; set; }
    public bool IsDisabled { get; set; }
    public string CronExpression { get; set; }
    public DateTimeOffset NextRunAtUtc { get; set; }
    public string SelectionStrategy { get; set; }
    public string Status { get; set; }
    public DateTimeOffset? LastRunStartedAtUtc { get; set; }
    public DateTimeOffset? LastRunCompletedAtUtc { get; set; }
    public IEnumerable<Guid> ScraperIds { get; set; }
}

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
    Core.Domain.Entities.ScheduledTaskStatus? Status,
    List<Guid>? ScraperIds);

public record UpdateScheduledTaskScrapersRequest(
    List<Guid>? Add,
    List<Guid>? Remove);

public record TransferScheduledTaskScrapersRequest(
    Guid SourceTaskId,
    List<Guid>? ScraperIds,
    string? Mode);