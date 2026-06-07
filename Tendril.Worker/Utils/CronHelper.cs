using Cronos;
using Tendril.Core.Domain.Entities;

namespace Tendril.Worker.Utils;

public static class CronHelper
{
    public static void CalculateNextRun(ScheduledTask task, DateTimeOffset fromTime)
    {
        try
        {
            var expression = CronExpression.Parse(task.CronExpression);
            var nextOccurrence = expression.GetNextOccurrence(fromTime.UtcDateTime);

            if (nextOccurrence.HasValue)
            {
                task.NextRunAtUtc = new DateTimeOffset(nextOccurrence.Value, TimeSpan.Zero);
                logger.LogInformation("Task '{Name}' rescheduled for {NextRun}", task.Name, task.NextRunAtUtc);
            }
            else
            {
                task.IsDisabled = true;
                logger.LogWarning("Task '{Name}' has a CRON expression that yields no future occurrences. Disabling task.", task.Name);
            }
        }
        catch (CronFormatException ex)
        {
            task.IsDisabled = true;
            task.Status = ScheduledTaskStatus.InvalidCron;
            logger.LogError(ex, "Task '{Name}' has a malformed CRON expression. Disabling task.", task.Name);
        }
    }
}
