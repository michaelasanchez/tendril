using Cronos;

namespace Tendril.Worker.Utils;

public enum NextRunResult
{
    Scheduled,
    NoFutureOccurrence,
    InvalidExpression
}

public static class CronHelper
{
    /// <summary>
    /// Pure calculation: the worker applies the result with targeted updates, so it never
    /// mutates (or saves) a tracked ScheduledTask.
    /// </summary>
    public static NextRunResult TryGetNextRun(
        string cronExpression, DateTimeOffset fromTime, out DateTimeOffset nextRunAtUtc)
    {
        nextRunAtUtc = default;

        if (string.IsNullOrWhiteSpace(cronExpression))
            return NextRunResult.InvalidExpression;

        try
        {
            var expression = CronExpression.Parse(cronExpression);
            var next = expression.GetNextOccurrence(fromTime.UtcDateTime);

            if (next is null)
                return NextRunResult.NoFutureOccurrence;

            nextRunAtUtc = new DateTimeOffset(next.Value, TimeSpan.Zero);
            return NextRunResult.Scheduled;
        }
        catch (CronFormatException)
        {
            return NextRunResult.InvalidExpression;
        }
    }
}
