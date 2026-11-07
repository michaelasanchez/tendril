namespace Tendril.Core.Domain.Entities;

public class WorkerSession
{
    public Guid Id { get; set; }
    public string MachineName { get; set; } = null!;
    public int ProcessId { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset LastHeartbeatAtUtc { get; set; }
    public DateTimeOffset? StoppedAtUtc { get; set; }
}