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
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("Scheduled Task Worker started.");
        using var periodicTimer = new PeriodicTimer(HeartbeatInterval);

        while (await periodicTimer.WaitForNextTickAsync(ct))
        {
            try
            {
                await PollAndExecuteTasksAsync(ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during the scheduled task polling cycle.");
            }
        }
    }

    private async Task PollAndExecuteTasksAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();

        var taskRepo = scope.ServiceProvider.GetRequiredService<IScheduledTaskRepository>();
        var taskRunRepo = scope.ServiceProvider.GetRequiredService<IScheduledTaskRunRepository>();
        var scraperRepo = scope.ServiceProvider.GetRequiredService<IScraperRepository>();
        var ingestionService = scope.ServiceProvider.GetRequiredService<IIngestionService>();

        var now = DateTimeOffset.UtcNow;
        var allTasks = await taskRepo.GetAllAsync(ct);

        // We pick up tasks where NextRun is due OR tasks that are currently "Running" but crashed/failed previously
        var pendingTasks = allTasks.Where(t => !t.IsDisabled && (t.NextRunAtUtc <= now || t.Status == ScheduledTaskStatus.Running || t.Status == ScheduledTaskStatus.Retrying)).ToList();

        if (pendingTasks.Count == 0) return;

        foreach (var task in pendingTasks)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                task.Status = ScheduledTaskStatus.Running;
                await taskRepo.UpdateAsync(task, ct);

                // 1. Get or Create the current Task Run
                var currentRun = await taskRunRepo.GetIncompleteRunAsync(task.Id, ct);
                if (currentRun == null)
                {
                    currentRun = new ScheduledTaskRun
                    {
                        Id = Guid.NewGuid(),
                        ScheduledTaskId = task.Id,
                        StartTimeUtc = now,
                        Status = RunStatus.Running, // Assuming string based on your DB config, adjust to RunStatus.Running if you strictly use the Enum
                        RetryCount = 0      // Tracks how many times this specific run has retried
                    };
                    await taskRunRepo.AddAsync(currentRun, ct);
                }

                // 2. Resolve Scrapers
                var scrapersToRun = new List<ScraperDefinition>();

                if (task.SelectionStrategy == SelectionStrategy.All)
                {
                    var allScrapers = await scraperRepo.GetAllWithDetailsAsync(ct);
                    scrapersToRun = allScrapers.Where(s => s.IsEventFeed).ToList();
                }
                else if (task.SelectionStrategy == SelectionStrategy.Selected)
                {
                    var fullTaskScrapers = await taskRepo.GetScrapersForTaskAsync(task, ct);
                    if (fullTaskScrapers != null)
                    {
                        scrapersToRun = fullTaskScrapers.Where(sa => sa.IsEventFeed).ToList();
                    }
                }

                // 3. Filter out scrapers that ALREADY succeeded in this specific run
                var successfulScraperIdsInRun = currentRun.AttemptHistories
                    .Where(a => a.Success)
                    .Select(a => a.ScraperDefinitionId)
                    .ToHashSet();

                var scrapersNeedingExecution = scrapersToRun
                    .Where(s => !successfulScraperIdsInRun.Contains(s.Id))
                    .ToList();

                logger.LogInformation("Task '{TaskName}': {Count} scrapers total, {Pending} pending execution.",
                    task.Name, scrapersToRun.Count, scrapersNeedingExecution.Count);

                // 4. Execute Pending Scrapers
                foreach (var scraper in scrapersNeedingExecution)
                {
                    await ingestionService.Ingest(scraper, currentRun.Id, ct);
                }

                // 5. Evaluate overall Run Success
                var refreshedRun = await taskRunRepo.GetByIdWithAttemptsAsync(currentRun.Id, ct) ?? currentRun;

                // Did EVERY scraper mapped to this task succeed?
                bool allSucceeded = true;
                foreach (var mappedScraper in scrapersToRun)
                {
                    bool succeeded = refreshedRun.AttemptHistories.Any(a => a.ScraperDefinitionId == mappedScraper.Id && a.Success);
                    if (!succeeded)
                    {
                        allSucceeded = false;
                        break;
                    }
                }

                if (allSucceeded)
                {
                    // SUCCESS! Close out the run and calculate next cron.
                    refreshedRun.Status = RunStatus.Completed;
                    refreshedRun.EndTimeUtc = DateTimeOffset.UtcNow;
                    await taskRunRepo.UpdateAsync(refreshedRun, ct);

                    task.Status = ScheduledTaskStatus.Idle;
                    CalculateNextRun(task, now);
                }
                else
                {
                    // FAILURE (One or more scrapers failed)
                    if (refreshedRun.RetryCount >= task.MaxRetries)
                    {
                        // Exhausted retries. Mark as failed and move on to the next schedule.
                        logger.LogWarning("Task '{TaskName}' exhausted all {Max} retries. Marking run as Failed.", task.Name, task.MaxRetries);

                        refreshedRun.Status = RunStatus.Failed;
                        refreshedRun.EndTimeUtc = DateTimeOffset.UtcNow;
                        await taskRunRepo.UpdateAsync(refreshedRun, ct);

                        task.Status = ScheduledTaskStatus.Idle;
                        CalculateNextRun(task, now); // Move on so we don't get stuck forever
                    }
                    else
                    {
                        // Leave the run open, increment retry count. Do NOT bump NextRunAtUtc!
                        refreshedRun.Status = RunStatus.Retrying;
                        refreshedRun.RetryCount++;
                        await taskRunRepo.UpdateAsync(refreshedRun, ct);

                        task.Status = ScheduledTaskStatus.Retrying;
                        logger.LogInformation("Task '{TaskName}' failed to process all scrapers. Retrying (Attempt {RetryCount}/{MaxRetries}) on next heartbeat.", task.Name, refreshedRun.RetryCount, task.MaxRetries);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fatal error executing task {TaskId}", task.Id);
                task.Status = ScheduledTaskStatus.Error; // Put task in an error state to be picked up next loop
            }
            finally
            {
                await taskRepo.UpdateAsync(task, ct);
            }
        }
    }

    private void CalculateNextRun(ScheduledTask task, DateTimeOffset fromTime)
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