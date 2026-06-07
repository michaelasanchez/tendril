using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Core.Interfaces.Repositories;
using Tendril.Engine.Abstractions;
using Tendril.Worker.Utils;

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
        var now = DateTimeOffset.UtcNow;
        List<ScheduledTask> pendingTasks;

        // Create a short-lived scope just to look up what needs work
        using (var scope = serviceProvider.CreateScope())
        {
            var taskRepo = scope.ServiceProvider.GetRequiredService<IScheduledTaskRepository>();
            var allTasks = await taskRepo.GetAllAsync(ct);

            pendingTasks = allTasks.Where(t => !t.IsDisabled &&
                (t.NextRunAtUtc <= now ||
                 t.Status == ScheduledTaskStatus.Running ||
                 t.Status == ScheduledTaskStatus.Retrying)).ToList();
        }

        if (pendingTasks.Count == 0) return;

        foreach (var pendingTask in pendingTasks)
        {
            ct.ThrowIfCancellationRequested();

            using var scope = serviceProvider.CreateScope();

            var (taskRepo, taskRunRepo, scraperRepo, ingestionService) = GetServices(scope);

            // Re-fetch or attach the task to the fresh context
            var task = await taskRepo.GetByIdAsync(pendingTask.Id, ct);
            if (task == null) continue;

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
                        StartTimeUtc = DateTimeOffset.UtcNow,
                        Status = RunStatus.Running,
                        RetryCount = 0
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

                // 3. Filter out scrapers that ALREADY succeeded (Fresh read from fresh context)
                var successfulScraperIdsInRun = currentRun.AttemptHistories
                    .Where(a => a.Success)
                    .Select(a => a.ScraperDefinitionId)
                    .ToHashSet();

                var scrapersNeedingExecution = scrapersToRun
                    .Where(s => !successfulScraperIdsInRun.Contains(s.Id))
                    .ToList();

                logger.LogInformation("Task '{TaskName}': {Count} scrapers total, {Pending} pending execution.",
                    task.Name, scrapersToRun.Count, scrapersNeedingExecution.Count);

                // 4. Execute Pending Scrapers (Consider Task.WhenAll here if concurrency is safe)
                foreach (var scraper in scrapersNeedingExecution)
                {
                    try
                    {
                        await ingestionService.Ingest(scraper, currentRun.Id, ct);
                    }
                    catch (Exception scraperEx)
                    {
                        logger.LogError(scraperEx, "Scraper {ScraperId} failed during execution.", scraper.Id);
                        // Catching locally so one broken scraper doesn't stop other pending ones in the same run
                    }
                }

                // 5. Evaluate overall Run Success
                var refreshedRun = await taskRunRepo.GetByIdWithAttemptsAsync(currentRun.Id, ct) ?? currentRun;

                bool allSucceeded = scrapersToRun.All(mappedScraper =>
                    refreshedRun.AttemptHistories.Any(a => a.ScraperDefinitionId == mappedScraper.Id && a.Success));

                if (allSucceeded)
                {
                    refreshedRun.Status = RunStatus.Completed;
                    refreshedRun.EndTimeUtc = DateTimeOffset.UtcNow;
                    await taskRunRepo.UpdateAsync(refreshedRun, ct);

                    task.Status = ScheduledTaskStatus.Idle;
                    CronHelper.CalculateNextRun(task, DateTimeOffset.UtcNow);
                }
                else
                {
                    if (refreshedRun.RetryCount >= task.MaxRetries)
                    {
                        logger.LogWarning("Task '{TaskName}' exhausted all {Max} retries.", task.Name, task.MaxRetries);

                        refreshedRun.Status = RunStatus.Failed;
                        refreshedRun.EndTimeUtc = DateTimeOffset.UtcNow;
                        await taskRunRepo.UpdateAsync(refreshedRun, ct);

                        task.Status = ScheduledTaskStatus.Idle;
                        CronHelper.CalculateNextRun(task, DateTimeOffset.UtcNow);
                    }
                    else
                    {
                        refreshedRun.Status = RunStatus.Retrying;
                        refreshedRun.RetryCount++;
                        await taskRunRepo.UpdateAsync(refreshedRun, ct);

                        task.Status = ScheduledTaskStatus.Retrying;

                        // OPTIONAL: Back-off logic instead of instant 30s retry
                        task.NextRunAtUtc = DateTimeOffset.UtcNow.AddMinutes(Math.Pow(2, refreshedRun.RetryCount));

                        logger.LogInformation("Task '{TaskName}' failed to process all scrapers. Retrying ({RetryCount}/{MaxRetries}).",
                            task.Name, refreshedRun.RetryCount, task.MaxRetries);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fatal error executing task {TaskId}", task.Id);
                task.Status = ScheduledTaskStatus.Error;
            }
            finally
            {
                await taskRepo.UpdateAsync(task, ct);
            }
        }
    }

    private record Services
    (
        IScheduledTaskRepository TaskRepo,
        IScheduledTaskRunRepository TaskRunRepo,
        IScraperRepository ScraperRepo,
        IIngestionService IngestionService
    );

    private Services GetServices(IServiceScope? scope)
    {
        var taskRepo = scope.ServiceProvider.GetRequiredService<IScheduledTaskRepository>();
        var taskRunRepo = scope.ServiceProvider.GetRequiredService<IScheduledTaskRunRepository>();
        var scraperRepo = scope.ServiceProvider.GetRequiredService<IScraperRepository>();
        var ingestionService = scope.ServiceProvider.GetRequiredService<IIngestionService>();

        return new(taskRepo, taskRunRepo, scraperRepo, ingestionService);
    }
}