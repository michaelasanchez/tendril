using Microsoft.Extensions.Options;
using Tendril.Core.Domain;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;
using Tendril.Engine.Abstractions;
using Tendril.Engine.Playwright;
using Tendril.Worker.Infrastructure;
using Tendril.Worker.Utils;

namespace Tendril.Worker;

public sealed class Worker(
    ILogger<Worker> logger,
    IServiceScopeFactory scopeFactory,
    TimeProvider time,
    PlaywrightContextFactory browsers,
    IOptions<BrowserOptions> browserOptions) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(60);

    private enum OutcomeKind
    {
        Reschedule,   // run finished (completed or out of retries): next run comes from the cron
        Retry,        // run unfinished: try again after a backoff
        Error,        // unexpected failure: needs a manual reset
        LeaveRunning  // shutdown mid-task: startup recovery resumes it
    }

    private readonly record struct TaskOutcome(OutcomeKind Kind, DateTimeOffset? RetryAtUtc = null);

    // Identifies the headful Chrome this process launched (trailing space avoids port prefixes).
    private string DebugPortNeedle => $"--remote-debugging-port={browserOptions.Value.DebugPort} ";

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("Scheduled Task Worker started.");

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
        // A Chrome we launched can outlive a crashed worker. Port-specific, so it never
        // touches another process's browser (for example the API's test runs).
        BrowserProcessSweeper.KillByCommandLineMatch(DebugPortNeedle, logger);

        await using var scope = scopeFactory.CreateAsyncScope();
        var taskRepo = scope.ServiceProvider.GetRequiredService<IScheduledTaskRepository>();
        var runRepo = scope.ServiceProvider.GetRequiredService<IScheduledTaskRunRepository>();

        var now = time.GetUtcNow();

        var closed = await runRepo.CloseInterruptedAttemptsAsync(now, ct);
        if (closed > 0)
            logger.LogWarning("Closed {Count} attempt(s) interrupted by the previous shutdown.", closed);

        var recovered = await taskRepo.RecoverOrphanedRunningTasksAsync(now, ct);
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
        // The worker never tracks a ScheduledTask: it reads this snapshot and writes targeted
        // updates, so UI edits made mid-run are not overwritten and RowVersion can't conflict.
        ScheduledTaskSnapshot? task;
        await using (var lookup = scopeFactory.CreateAsyncScope())
        {
            var repo = lookup.ServiceProvider.GetRequiredService<IScheduledTaskRepository>();
            task = await repo.GetSnapshotAsync(taskId, ct);
        }

        if (task is null || task.IsDisabled) return;

        using var logScope = logger.BeginScope(new Dictionary<string, object>
        {
            ["TaskId"] = task.Id,
            ["TaskName"] = task.Name
        });

        // Scope for the task-level bookkeeping (run row). Each scraper gets its own scope below.
        await using var scope = scopeFactory.CreateAsyncScope();
        var taskRepo = scope.ServiceProvider.GetRequiredService<IScheduledTaskRepository>();
        var runRepo = scope.ServiceProvider.GetRequiredService<IScheduledTaskRunRepository>();

        ScheduledTaskRun? run = null;
        var outcome = new TaskOutcome(OutcomeKind.Error); // pessimistic default; every path sets it

        try
        {
            await taskRepo.MarkRunStartedAsync(task.Id, time.GetUtcNow(), ct);

            run = await GetOrCreateRunAsync(task.Id, runRepo, ct);

            var scraperIds = await taskRepo.GetEventFeedScraperIdsAsync(task.Id, task.SelectionStrategy, ct);
            if (scraperIds.Count == 0)
            {
                // Counts as Completed (nothing to fail). Change FinalizeRunAsync to treat it as a failure.
                logger.LogWarning("Task resolved zero event-feed scrapers (strategy {Strategy}).",
                    task.SelectionStrategy);
            }

            var alreadySucceeded = await runRepo.GetSucceededScraperIdsAsync(run.Id, ct);
            var pending = scraperIds.Where(id => !alreadySucceeded.Contains(id)).ToList();

            logger.LogInformation("{Total} scrapers total, {Pending} pending execution.",
                scraperIds.Count, pending.Count);

            var missing = new HashSet<Guid>(); // deleted since the task was configured
            foreach (var scraperId in pending)
            {
                ct.ThrowIfCancellationRequested();

                if (!await RunScraperAsync(scraperId, run.Id, ct))
                    missing.Add(scraperId);
            }

            outcome = await FinalizeRunAsync(task, run, scraperIds, missing, runRepo, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Shutdown mid-task. Status stays Running; RecoverOnStartupAsync resumes it.
            logger.LogInformation("Shutdown requested during task; leaving it for recovery.");
            outcome = new TaskOutcome(OutcomeKind.LeaveRunning);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error executing task.");
            outcome = new TaskOutcome(OutcomeKind.Error);

            if (run is not null)
            {
                try
                {
                    run.Status = RunStatus.Failed;
                    run.EndTimeUtc = time.GetUtcNow();
                    await runRepo.UpdateAsync(run, CancellationToken.None);
                }
                catch (Exception runEx)
                {
                    logger.LogError(runEx, "Could not mark run {RunId} as failed.", run.Id);
                }
            }
        }
        finally
        {
            await ApplyOutcomeAsync(task.Id, outcome);

            try { await browsers.RecycleAsync(); }
            catch (Exception ex) { logger.LogWarning(ex, "Browser recycle failed."); }
        }
    }

    // ---------------------------------------------------------------- one scraper

    /// <returns>false when the scraper no longer exists.</returns>
    private async Task<bool> RunScraperAsync(Guid scraperId, Guid runId, CancellationToken ct)
    {
        // A scope (and DbContext) per scraper: the change tracker doesn't grow across a whole
        // task, and a failed save can't poison the scrapers that run after it.
        await using var scope = scopeFactory.CreateAsyncScope();
        var scraperRepo = scope.ServiceProvider.GetRequiredService<IScraperRepository>();
        var ingestion = scope.ServiceProvider.GetRequiredService<IIngestionService>();

        // Loaded with Parameters, Actions and rules: the executor and mapper depend on them.
        var scraper = await scraperRepo.GetByIdWithDetailsAsync(scraperId, ct);
        if (scraper is null)
        {
            logger.LogWarning("Scraper {ScraperId} no longer exists; skipping.", scraperId);
            return false;
        }

        try
        {
            await ingestion.Ingest(scraper, runId, ct);

            // Ingest records a shutdown as a cancelled attempt and returns normally.
            ct.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // real shutdown: don't count it as a scraper failure
        }
        catch (Exception ex)
        {
            // One broken scraper shouldn't stop the rest of this run.
            logger.LogError(ex, "Scraper {ScraperId} failed during execution.", scraperId);
        }
        finally
        {
            LogMemory(scraperId);

            try { await browsers.StopPersistentChromeAsync(); }
            catch (Exception ex) { logger.LogWarning(ex, "Could not stop Chrome after scraper {ScraperId}.", scraperId); }
        }

        return true;
    }

    private void LogMemory(Guid scraperId)
    {
        try
        {
            // Headful Chrome only (matched by debug port). Trend over weeks reveals leaks.
            logger.LogInformation(
                "After scraper {ScraperId}: chrome={ChromeMb} MB, dotnet={DotnetMb} MB",
                scraperId,
                BrowserProcessSweeper.GetWorkingSetMb(DebugPortNeedle),
                GC.GetTotalMemory(false) / (1024 * 1024));
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not read browser memory.");
        }
    }

    // ---------------------------------------------------------------- run bookkeeping

    private async Task<ScheduledTaskRun> GetOrCreateRunAsync(
        Guid taskId, IScheduledTaskRunRepository runRepo, CancellationToken ct)
    {
        // Returns only runs still Running/Retrying, so a finished (Failed) run is never resumed.
        var existing = await runRepo.GetIncompleteRunAsync(taskId, ct);
        if (existing is not null) return existing;

        var run = new ScheduledTaskRun
        {
            Id = Guid.NewGuid(),
            ScheduledTaskId = taskId,
            StartTimeUtc = time.GetUtcNow(),
            Status = RunStatus.Running,
            RetryCount = 0
        };
        await runRepo.AddAsync(run, ct);
        return run;
    }

    private async Task<TaskOutcome> FinalizeRunAsync(
        ScheduledTaskSnapshot task,
        ScheduledTaskRun run,
        IReadOnlyCollection<Guid> scraperIds,
        HashSet<Guid> missing,
        IScheduledTaskRunRepository runRepo,
        CancellationToken ct)
    {
        var now = time.GetUtcNow();

        // Fresh read: attempts were written by the per-scraper contexts.
        var succeeded = await runRepo.GetSucceededScraperIdsAsync(run.Id, ct);
        var failed = scraperIds.Where(id => !succeeded.Contains(id) && !missing.Contains(id)).ToList();

        if (failed.Count == 0)
        {
            run.Status = RunStatus.Completed;
            run.EndTimeUtc = now;
            await runRepo.UpdateAsync(run, ct);

            return new TaskOutcome(OutcomeKind.Reschedule);
        }

        if (run.RetryCount >= task.MaxRetries)
        {
            logger.LogWarning("Task exhausted all {Max} retries. Still failing: {Failed}.",
                task.MaxRetries, failed);

            run.Status = RunStatus.Failed;
            run.EndTimeUtc = now;
            await runRepo.UpdateAsync(run, ct);

            return new TaskOutcome(OutcomeKind.Reschedule);
        }

        run.Status = RunStatus.Retrying;
        run.RetryCount++;
        await runRepo.UpdateAsync(run, ct);

        // 2, 4, 8, ... minutes, capped.
        var delayMinutes = Math.Min(Math.Pow(2, run.RetryCount), MaxBackoff.TotalMinutes);

        logger.LogInformation("{FailedCount} scraper(s) did not succeed. Retry {Retry}/{Max} in {Delay} min.",
            failed.Count, run.RetryCount, task.MaxRetries, delayMinutes);

        return new TaskOutcome(OutcomeKind.Retry, now.AddMinutes(delayMinutes));
    }

    // ---------------------------------------------------------------- applying the outcome

    /// <summary>
    /// Targeted updates with CancellationToken.None (ct may already be cancelled on shutdown).
    /// The cron and disabled flag are re-read, so a change made in the UI mid-run is honored.
    /// </summary>
    private async Task ApplyOutcomeAsync(Guid taskId, TaskOutcome outcome)
    {
        try
        {
            if (outcome.Kind == OutcomeKind.LeaveRunning) return;

            await using var scope = scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IScheduledTaskRepository>();
            var now = time.GetUtcNow();

            switch (outcome.Kind)
            {
                case OutcomeKind.Error:
                    await repo.SetStatusAsync(taskId, ScheduledTaskStatus.Error, CancellationToken.None);
                    break;

                case OutcomeKind.Retry:
                    await repo.SaveScheduleAsync(taskId, ScheduledTaskStatus.Retrying,
                        outcome.RetryAtUtc!.Value, now, CancellationToken.None);
                    break;

                case OutcomeKind.Reschedule:
                    var fresh = await repo.GetSnapshotAsync(taskId, CancellationToken.None);
                    if (fresh is null) return; // deleted while running

                    switch (CronHelper.TryGetNextRun(fresh.CronExpression, now, out var next))
                    {
                        case NextRunResult.Scheduled:
                            await repo.SaveScheduleAsync(taskId, ScheduledTaskStatus.Idle,
                                next, now, CancellationToken.None);
                            break;

                        case NextRunResult.NoFutureOccurrence:
                            logger.LogWarning("Cron '{Cron}' has no future occurrences; disabling task.",
                                fresh.CronExpression);
                            await repo.DisableAsync(taskId, ScheduledTaskStatus.Idle, now, CancellationToken.None);
                            break;

                        default:
                            logger.LogError("Cron '{Cron}' is malformed; disabling task.", fresh.CronExpression);
                            await repo.DisableAsync(taskId, ScheduledTaskStatus.InvalidCron, now, CancellationToken.None);
                            break;
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            // The task stays Running until the next restart's recovery picks it up.
            logger.LogError(ex, "Could not persist final state for task {TaskId}.", taskId);
        }
    }
}

public sealed class WorkerSessionHostedService(
    ILogger<WorkerSessionHostedService> logger,
    IServiceScopeFactory scopeFactory,
    TimeProvider time) : BackgroundService
{
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var startedAt = time.GetUtcNow();
        var session = new WorkerSession
        {
            Id = Guid.NewGuid(),
            MachineName = Environment.MachineName,
            ProcessId = Environment.ProcessId,
            StartedAtUtc = startedAt,
            LastHeartbeatAtUtc = startedAt
        };

        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IWorkerSessionRepository>();
            await repository.StartAsync(session, stoppingToken);
        }

        logger.LogInformation("Worker session {SessionId} started on {MachineName} as process {ProcessId}.",
            session.Id, session.MachineName, session.ProcessId);

        try
        {
            using var timer = new PeriodicTimer(HeartbeatInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IWorkerSessionRepository>();
                    await repository.HeartbeatAsync(session.Id, time.GetUtcNow(), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Could not update heartbeat for worker session {SessionId}.", session.Id);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<IWorkerSessionRepository>();
                await repository.StopAsync(session.Id, time.GetUtcNow(), CancellationToken.None);
                logger.LogInformation("Worker session {SessionId} stopped.", session.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not mark worker session {SessionId} as stopped.", session.Id);
            }
        }
    }
}
