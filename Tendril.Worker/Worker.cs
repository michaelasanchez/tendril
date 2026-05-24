using Cronos;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Core.Interfaces.Repositories;
using Tendril.Engine.Abstractions;

namespace Tendril.Worker;

public sealed class Worker(
    ILogger<Worker> logger,
    IServiceProvider serviceProvider) : BackgroundService
{
    // A 30-second heartbeat ensures high precision without overloading your database
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("Scheduled Task Worker started.");

        using var periodicTimer = new PeriodicTimer(HeartbeatInterval);

        // periodicTimer.WaitForNextTickAsync respects cancellation tokens cleanly
        while (await periodicTimer.WaitForNextTickAsync(ct))
        {
            try
            {
                await PollAndExecuteTasksAsync(ct);
            }
            catch (OperationCanceledException)
            {
                // Clean exit when the worker is shutting down
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during the scheduled task polling cycle.");
            }
        }
    }

    private async Task PollAndExecuteTasksAsync(CancellationToken ct)
    {
        // Create a scope to safely resolve your DbContext-backed repositories
        using var scope = serviceProvider.CreateScope();

        var taskRepository = scope.ServiceProvider.GetRequiredService<IScheduledTaskRepository>();
        var scraperRepository = scope.ServiceProvider.GetRequiredService<IScraperRepository>();
        var ingestionService = scope.ServiceProvider.GetRequiredService<IIngestionService>();

        var now = DateTimeOffset.UtcNow;

        // 1. Fetch only enabled tasks that are due (or overdue)
        // Add a method to your repository interface if needed, e.g., GetPendingTasksAsync(now, ct)
        var allTasks = await taskRepository.GetAllAsync(ct);
        var pendingTasks = allTasks.Where(t => !t.IsDisabled && t.NextRunAtUtc <= now).ToList();

        if (pendingTasks.Count == 0) return;

        logger.LogInformation("Found {Count} tasks ready for execution.", pendingTasks.Count);

        foreach (var task in pendingTasks)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                // 2. Transition status to prevent double-execution in concurrent environments
                task.Status = "Running";
                await taskRepository.UpdateAsync(task, ct);

                // 3. Resolve Scrapers based on SelectionStrategy
                List<ScraperDefinition> scrapersToRun = [];

                if (task.SelectionStrategy == SelectionStrategy.All)
                {
                    // If "All", fetch everything available
                    var allScrapers = await scraperRepository.GetAllWithDetailsAsync(ct);
                    scrapersToRun = allScrapers.Where(s => s.IsEventFeed).ToList();
                }
                else if (task.SelectionStrategy == SelectionStrategy.Selected)
                {
                    // Ensure your repository populated ScraperDefinitions during retrieval
                    // If taskRepository.GetAllAsync doesn't include them, fetch this specific task explicitly
                    var fullTask = await taskRepository.GetByIdWithScrapersAsync(task.Id, ct);
                    if (fullTask != null)
                    {
                        scrapersToRun = fullTask.ScraperDefinitions.Where(s => s.IsEventFeed).ToList();
                    }
                }

                // 4. Execute Ingestion
                logger.LogInformation("Executing task '{TaskName}' spanning {Count} scrapers.", task.Name, scrapersToRun.Count);

                foreach (var scraper in scrapersToRun)
                {
                    await ingestionService.Ingest(scraper, ct);
                }

                // 5. Success Tracking & Schedule Next Run
                task.Status = "Idle";
                CalculateNextRun(task, now);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to complete execution for task {TaskId} ({TaskName})", task.Id, task.Name);
                task.Status = "Failed";

                // Still calculate next run so a single crash doesn't permanently stall the task
                CalculateNextRun(task, now);
            }
            finally
            {
                // Save the status and updated NextRunAtUtc changes back to the database
                await taskRepository.UpdateAsync(task, ct);
            }
        }
    }

    private void CalculateNextRun(ScheduledTask task, DateTimeOffset fromTime)
    {
        try
        {
            // Parses standard CRON (e.g., "0 * * * *" for hourly)
            var cron = CronExpression.Parse(task.CronExpression);
            var nextUtc = cron.GetNextOccurrence(fromTime.UtcDateTime);

            if (nextUtc.HasValue)
            {
                task.NextRunAtUtc = new DateTimeOffset(nextUtc.Value, TimeSpan.Zero);
                logger.LogInformation("Task '{Name}' rescheduled for {NextRun}", task.Name, task.NextRunAtUtc);
            }
            else
            {
                // Fallback protection if CRON won't fire again
                task.IsDisabled = true;
                logger.LogWarning("Task '{Name}' has a CRON expression that yields no future occurrences. Disabling task.", task.Name);
            }
        }
        catch (CronFormatException ex)
        {
            task.IsDisabled = true;
            task.Status = "InvalidCron";
            logger.LogError(ex, "Task '{Name}' has a malformed CRON expression. Disabling task.", task.Name);
        }
    }
}