namespace Tendril.Engine.Utils;

public class YearTracker
{
    public int CurrentYear { get; private set; }
    private int? _lastSeenMonth;
    private readonly DateTimeOffset _initTime;

    public YearTracker(DateTimeOffset? startTime = null)
    {
        _initTime = startTime ?? DateTimeOffset.UtcNow;
        CurrentYear = _initTime.Year;
    }

    /// <summary>
    /// Processes a month and returns the correctly assigned year.
    /// Handles year rollovers and initial "next year" detection.
    /// </summary>
    public int ProcessMonth(int month)
    {
        // 1. Initial Guess: If the first event we see is "earlier" than our 
        // current month (e.g., it's Dec and we see Jan), it's for next year.
        if (!_lastSeenMonth.HasValue)
        {
            if (month < _initTime.Month - 1) // Tolerance of 1 month for late-night scrapes
            {
                CurrentYear++;
            }
        }
        // 2. Rollover Detection: If the month decreases (e.g., 12 -> 1), 
        // we've crossed into the next year.
        else if (month < _lastSeenMonth.Value)
        {
            CurrentYear++;
        }

        _lastSeenMonth = month;

        return CurrentYear;
    }
}