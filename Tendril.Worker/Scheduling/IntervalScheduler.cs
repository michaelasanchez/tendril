namespace Tendril.Worker.Scheduling;

public class IntervalScheduler
{
    public static async Task WaitAsync(int seconds, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // ignore
        }
    }
}
