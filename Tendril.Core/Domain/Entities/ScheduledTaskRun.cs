namespace Tendril.Core.Domain.Entities;

public enum RunStatus
{
    Running,
    Completed,
    Retrying,
    Failed
}


public class ScheduledTaskRun
{
    public Guid Id { get; set; }
    public Guid ScheduledTaskId { get; set; }
    public ScheduledTask ScheduledTask { get; set; } = null!;

    public DateTimeOffset StartTimeUtc { get; set; }
    public DateTimeOffset? EndTimeUtc { get; set; }

    public RunStatus Status { get; set; } = RunStatus.Running;
    public int RetryCount { get; set; } = 0;

    public ICollection<ScraperAttemptHistory> AttemptHistories { get; set; } = [];
}
