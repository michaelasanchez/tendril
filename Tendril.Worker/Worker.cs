using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Core.Interfaces.Repositories;
using Tendril.Engine.Abstractions;
using Tendril.Worker.Infrastructure;
using Tendril.Worker.Utils;

namespace Tendril.Worker;

public sealed class Worker(
    ILogger<Worker> logger,
    IServiceScopeFactory scopeFactory,   // was IServiceProvider
    TimeProvider time) : BackgroundService  // register: services.AddSingleton(TimeProvider.System)
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(60);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("Scheduled Task Worker started.");

        // Items 2 + 12: single process, so anything "Running" at boot is an orphan.
        try { await RecoverOnStartupAsync(ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
        catch (Exception ex) { logger.LogError(ex, "Startup recovery failed."); }

        using var timer = new PeriodicTimer(PollInterval);

        // Polls once immediately, then once per interval.
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await PollAndExecuteTasksAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during the scheduled task polling cycle.");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(ct)) break;
            }
            catch (OperationCanceledException) { break; }
        }

        logger.LogInformation("Scheduled Task Worker stopped.");
    }

    // ---------------------------------------------------------------- startup

    private async Task RecoverOnStartupAsync(CancellationToken ct)
    {
        // Kill leftover Chrome from a crash / power loss, then wipe stale profiles.
        BrowserProcessSweeper.KillByCommandLineMatch(BrowserPaths.ProfileRoot, logger);
        BrowserPaths.DeleteAllProfiles(logger);

        await using var scope = scopeFactory.CreateAsyncScope();
        var taskRepo = scope.ServiceProvider.GetRequiredService<IScheduledTaskRepository>();

        var recovered = await taskRepo.RecoverOrphanedRunningTasksAsync(time.GetUtcNow(), ct);
        if (recovered > 0)
            logger.LogWarning("Recovered {Count} task(s) left in Running state; they will resume.", recovered);
    }

    // ---------------------------------------------------------------- polling

    private async Task PollAndExecuteTasksAsync(CancellationToken ct)
    {
        List<Guid> dueIds;

        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var taskRepo = scope.ServiceProvider.GetRequiredService<IScheduledTaskRepository>();
            dueIds = await taskRepo.GetDueTaskIdsAsync(time.GetUtcNow(), ct);
        }

        foreach (var id in dueIds)
        {
            ct.ThrowIfCancellationRequested();
            await ExecuteTaskAsync(id, ct); // strictly sequential: one task at a time
        }
    }

    // ---------------------------------------------------------------- one task

    private async Task ExecuteTaskAsync(Guid taskId, CancellationToken ct)
    {
        // CreateAsyncScope (not CreateScope) so scoped IAsyncDisposable services,
        // such as a per-task browser, are disposed correctly.
        await using var scope = scopeFactory.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var taskRepo = sp.GetRequiredService<IScheduledTaskRepository>();
        var taskRunRepo = sp.GetRequiredService<IScheduledTaskRunRepository>();
        var scraperRepo = sp.GetRequiredService<IScraperRepository>();
        var ingestion = sp.GetRequiredService<IIngestionService>();

        var task = await taskRepo.GetByIdAsync(taskId, ct);
        if (task is null || task.IsDisabled) return;

        using var logScope = logger.BeginScope(new Dictionary<string, object>
        {
            ["TaskId"] = task.Id,
            ["TaskName"] = task.Name
        });

        ScheduledTaskRun? run = null;

        try
        {
            task.Status = ScheduledTaskStatus.Running;
            await taskRepo.UpdateAsync(task, ct);

            run = await GetOrCreateRunAsync(task, taskRunRepo, ct);

            var scrapersToRun = await ResolveScrapersAsync(task, taskRepo, scraperRepo, ct);
            if (scrapersToRun.Count == 0)
            {
                // Item 6. Currently falls through and counts as Completed.
                // Change FinalizeRunAsync if you'd rather treat this as a failure.
                logger.LogWarning("Task resolved zero event-feed scrapers (strategy {Strategy}).",
                    task.SelectionStrategy);
            }

            var succeededIds = run.AttemptHistories
                .Where(a => a.Success)
                .Select(a => a.ScraperDefinitionId)
                .ToHashSet();

            var pending = scrapersToRun.Where(s => !succeededIds.Contains(s.Id)).ToList();

            logger.LogInformation("{Total} scrapers total, {Pending} pending execution.",
                scrapersToRun.Count, pending.Count);

            foreach (var scraper in pending)
            {
                try
                {
                    await ingestion.Ingest(scraper, run.Id, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw; // real shutdown: don't count it as a scraper failure
                }
                catch (Exception scraperEx)
                {
                    // One broken scraper shouldn't stop the rest of this run.
                    logger.LogError(scraperEx, "Scraper {ScraperId} failed during execution.", scraper.Id);
                }

                // Item 17: leak trend over weeks of uptime.
                //logger.LogInformation(
                //    "After scraper {ScraperId}: chrome={ChromeMb} MB, dotnet={DotnetMb} MB",
                //    scraper.Id,
                //    BrowserProcessSweeper.GetWorkingSetMb(BrowserPaths.ProfileRoot),
                //    GC.GetTotalMemory(false) / (1024 * 1024));
            }

            run = await taskRunRepo.GetByIdWithAttemptsAsync(run.Id, ct) ?? run;
            await FinalizeRunAsync(task, run, scrapersToRun, taskRunRepo, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Shutdown mid-task. Status stays Running; RecoverOnStartupAsync resumes it.
            logger.LogInformation("Shutdown requested during task; leaving it for recovery.");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error executing task.");
            task.Status = ScheduledTaskStatus.Error; // excluded from polling, needs manual reset

            if (run is not null)
            {
                try
                {
                    run.Status = RunStatus.Failed;
                    run.EndTimeUtc = time.GetUtcNow();
                    await taskRunRepo.UpdateAsync(run, CancellationToken.None);
                }
                catch (Exception runEx)
                {
                    logger.LogError(runEx, "Could not mark run {RunId} as failed.", run.Id);
                }
            }
        }
        finally
        {
            try
            {
                // CancellationToken.None: ct may already be cancelled during shutdown.
                await taskRepo.UpdateAsync(task, CancellationToken.None);
            }
            catch (Exception saveEx)
            {
                logger.LogError(saveEx, "Could not persist final task state.");
            }
        }
    }

    // ---------------------------------------------------------------- helpers

    private async Task<ScheduledTaskRun> GetOrCreateRunAsync(
        ScheduledTask task, IScheduledTaskRunRepository runRepo, CancellationToken ct)
    {
        // Must return the run WITH AttemptHistories loaded (item 7), and must
        // include runs in both Running and Retrying status.
        var existing = await runRepo.GetIncompleteRunAsync(task.Id, ct);
        if (existing is not null) return existing;

        var run = new ScheduledTaskRun
        {
            Id = Guid.NewGuid(),
            ScheduledTaskId = task.Id,
            StartTimeUtc = time.GetUtcNow(),
            Status = RunStatus.Running,
            RetryCount = 0
        };
        await runRepo.AddAsync(run, ct);
        return run;
    }

    private static async Task<List<ScraperDefinition>> ResolveScrapersAsync(
        ScheduledTask task,
        IScheduledTaskRepository taskRepo,
        IScraperRepository scraperRepo,
        CancellationToken ct)
    {
        switch (task.SelectionStrategy)
        {
            case SelectionStrategy.All:
                var all = await scraperRepo.GetAllWithDetailsAsync(ct);
                return all.Where(s => s.IsEventFeed).ToList();

            case SelectionStrategy.Selected:
                var selected = await taskRepo.GetScrapersForTaskAsync(task, ct);
                return selected?.Where(s => s.IsEventFeed).ToList() ?? [];

            default:
                return [];
        }
    }

    private async Task FinalizeRunAsync(
        ScheduledTask task,
        ScheduledTaskRun run,
        List<ScraperDefinition> scrapersToRun,
        IScheduledTaskRunRepository runRepo,
        CancellationToken ct)
    {
        var now = time.GetUtcNow();

        var allSucceeded = scrapersToRun.All(s =>
            run.AttemptHistories.Any(a => a.ScraperDefinitionId == s.Id && a.Success));

        if (allSucceeded)
        {
            run.Status = RunStatus.Completed;
            run.EndTimeUtc = now;
            await runRepo.UpdateAsync(run, ct);

            task.Status = ScheduledTaskStatus.Idle;
            CronHelper.CalculateNextRun(task, now);
        }
        else if (run.RetryCount >= task.MaxRetries)
        {
            logger.LogWarning("Task exhausted all {Max} retries.", task.MaxRetries);

            run.Status = RunStatus.Failed;
            run.EndTimeUtc = now;
            await runRepo.UpdateAsync(run, ct);

            task.Status = ScheduledTaskStatus.Idle;
            CronHelper.CalculateNextRun(task, now);
        }
        else
        {
            run.Status = RunStatus.Retrying;
            run.RetryCount++;
            await runRepo.UpdateAsync(run, ct);

            // 2, 4, 8, ... minutes, capped. Respected now that the poll filters on NextRunAtUtc.
            var delayMinutes = Math.Min(Math.Pow(2, run.RetryCount), MaxBackoff.TotalMinutes);
            task.Status = ScheduledTaskStatus.Retrying;
            task.NextRunAtUtc = now.AddMinutes(delayMinutes);

            logger.LogInformation("Not all scrapers succeeded. Retry {Retry}/{Max} in {Delay} min.",
                run.RetryCount, task.MaxRetries, delayMinutes);
        }
    }
}