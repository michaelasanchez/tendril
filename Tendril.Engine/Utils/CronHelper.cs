using Cronos;

public static class CronHelper
{
    /// <summary>
    /// Calculates the next occurrence in UTC based on a 5-part CRON expression.
    /// Returns DateTimeOffset.UtcNow if the expression is invalid or has no future occurrences.
    /// </summary>
    public static DateTimeOffset GetNextRunUtc(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            return DateTimeOffset.UtcNow;
        }

        try
        {
            // CronFormat.Standard enforces the traditional 5-part format (Minute, Hour, Day, Month, Day of Week)
            var expression = CronExpression.Parse(cronExpression, CronFormat.Standard);

            // Get the next occurrence strictly after the current time in UTC
            var nextOccurrence = expression.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Utc);

            // If a future occurrence is found, return it. Otherwise, fallback to now.
            return nextOccurrence ?? DateTimeOffset.UtcNow;
        }
        catch (CronFormatException)
        {
            // Log this or handle it depending on your application's error strategy
            // Falling back to UtcNow keeps the task from stalling completely if the string is mangled
            return DateTimeOffset.UtcNow;
        }
    }
}