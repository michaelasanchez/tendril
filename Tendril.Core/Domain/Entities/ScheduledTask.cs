using Tendril.Core.Domain.Enums;

namespace Tendril.Core.Domain.Entities;

public enum ScheduledTaskStatus
{
    Idle,
    Running,
    Retrying,
    Error,
    InvalidCron
}

public class ScheduledTask
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Notes { get; set; }
    public bool IsDisabled { get; set; }

    // Scheduling configuration (e.g., standard cron string "0 */4 * * *")
    public string CronExpression { get; set; } = null!;
    public DateTimeOffset NextRunAtUtc { get; set; }

    // Control flag for your "All" vs "Selected" logic
    public SelectionStrategy SelectionStrategy { get; set; }
    public int MaxRetries { get; set; } = 3;

    // State Tracking & Concurrency
    public ScheduledTaskStatus Status { get; set; } = ScheduledTaskStatus.Idle; // Idle, Queued, Running
    public DateTimeOffset? LastRunStartedAtUtc { get; set; }
    public DateTimeOffset? LastRunCompletedAtUtc { get; set; }

    // Concurrency token to protect against multi-threaded or multi-instance worker assignment
    public byte[] RowVersion { get; set; } = [];

    // Relationships
    public ICollection<ScraperDefinition> ScraperDefinitions { get; set; } = [];

    public ICollection<ScheduledTaskRun> Runs { get; set; } = [];
}