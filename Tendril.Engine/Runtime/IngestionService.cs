using Microsoft.Extensions.Logging;
using System.Text.Json;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Core.Interfaces.Repositories;
using Tendril.Engine.Abstractions;
using Tendril.Engine.Interfaces;
using Tendril.Engine.Models;
using Tendril.Engine.Utils;

namespace Tendril.Engine.Runtime;

public class IngestionService(
    ILogger<IngestionService> logger,
    IAttemptHistoryRepository attemptHistories,
    IEventRepository events,
    IEventRevisionRepository eventRevisions,
    IRawEventRepository rawEvents,
    IScraperRepository scrapers,
    IMappingService mapper,
    IClassificationService classifier,
    IScrapeExecutor executor) : IIngestionService
{
    public async Task<IngestResult> Ingest(
        ScraperDefinition scraper,
        Guid? scheduledTaskRunId = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting ingestion for {Scraper}", scraper.Name);

        var start = DateTimeOffset.UtcNow;
        int created = 0, updated = 0, mapped = 0, extracted = 0, errored = 0, skipped = 0;

        var yearTracker = new YearTracker(DateTimeOffset.UtcNow);

        // 1. Create Attempt Record IMMEDIATELY (Mark as Running)
        var attempt = new ScraperAttemptHistory
        {
            Id = Guid.NewGuid(),
            ScraperDefinitionId = scraper.Id,
            ScheduledTaskRunId = scheduledTaskRunId,
            StartTimeUtc = start,
            Success = false,
            ErrorMessage = "Running..."
        };

        await attemptHistories.Add(attempt, cancellationToken);

        var errors = new List<string>();

        var rawEntities = new List<ScrapedEventRaw>();
        var mappedEntities = new List<Event>();
        var summaries = new List<string>();
        var context = new ScrapeContext();
        context.AddLog(ScraperAttemptLogLevel.Information,
            $"Started ingestion for scraper '{scraper.Name}' ({scraper.Id}, {scraper.ExecutionMode}).");

        try
        {
            // 2. Consume the Stream (Process 1 item at a time)
            await foreach (var raw in executor.RunScraperAsync(scraper, context, cancellationToken))
            {
                extracted++;

                // A. Save Raw Event
                var rawEntity = new ScrapedEventRaw
                {
                    Id = Guid.NewGuid(),
                    ScraperDefinitionId = scraper.Id,
                    ScraperAttemptHistoryId = attempt.Id,
                    ScrapedAtUtc = DateTimeOffset.UtcNow,
                    RawDataJson = JsonSerializer.Serialize(raw)
                };

                await rawEvents.AddAsync(rawEntity, cancellationToken);

                rawEntities.Add(rawEntity);

                // B. Map & Upsert Event
                try
                {
                    var (mappedEvent, status, summary) = await ProcessSingleEvent(
                        scraper,
                        attempt.Id,
                        rawEntity,
                        yearTracker.CurrentYear);

                    if (mappedEvent is not null)
                    {
                        mapped++;

                        if (mappedEvent.StartUtc != default)
                        {
                            int assignedYear = yearTracker.ProcessMonth(mappedEvent.StartUtc.Month);

                            if (scraper.UseYearTracking && assignedYear != mappedEvent.StartUtc.Year)
                            {
                                // If the tracker bumped the year, update the event objects
                                int diff = assignedYear - mappedEvent.StartUtc.Year;
                                mappedEvent.StartUtc = mappedEvent.StartUtc.AddYears(diff);

                                if (mappedEvent.EndUtc is not null && mappedEvent.EndUtc != default)
                                    mappedEvent.EndUtc = mappedEvent.EndUtc.Value.AddYears(diff);
                            }
                        }

                        if (scraper.RequiresReview)
                        {
                            mappedEvent.Status = EventStatus.Pending;
                        }

                        mappedEntities.Add(mappedEvent);
                    }

                    summaries.Add(summary);

                    if (status == "created") created++;
                    if (status == "updated") updated++;
                    if (status == "skipped") skipped++;
                }
                catch (Exception ex)
                {
                    rawEntity.Outcome = ScrapedEventOutcome.Failed;
                    rawEntity.ErrorMessage = ex.Message;

                    errors.Add(ex.Message);

                    errored++;

                    logger.LogError(ex, "Error mapping event");
                }
            }

            // 3. Success Completion
            scraper.State = ScraperState.Healthy;
            scraper.StateMessage = string.Empty;
            scraper.LastSuccessUtc = DateTimeOffset.UtcNow;

            attempt.Success = true;
            attempt.ErrorMessage = null;
        }
        catch (Exception ex)
        {
            // 4. Failure Completion
            var wasCanceled = ex is OperationCanceledException && cancellationToken.IsCancellationRequested;
            if (wasCanceled)
            {
                logger.LogInformation(ex, "Scraper job was canceled");
                context.AddLog(ScraperAttemptLogLevel.Warning,
                    $"Ingestion for scraper '{scraper.Name}' was canceled.");
            }
            else
            {
                logger.LogError(ex, "Scraper job failed");
                context.AddLog(ScraperAttemptLogLevel.Error,
                    $"Scraper '{scraper.Name}' failed: {ex.Message}");

                scraper.State = ScraperState.Unhealthy;
                scraper.StateMessage = $"Scraper failed: {ex.Message}";
                scraper.LastFailureUtc = DateTimeOffset.UtcNow;
                scraper.LastErrorMessage = ex.Message;
            }

            attempt.Success = false;
            attempt.ErrorMessage = wasCanceled ? "Ingestion canceled." : ex.Message;
        }
        finally
        {
            if (extracted == 0 && attempt.Success)
            {
                scraper.State = ScraperState.Warning;
                scraper.StateMessage = "Scraper completed with no events extracted";
            }

            // 5. Finalize Stats
            foreach (var logEntry in context.LogEntries)
            {
                logEntry.AttemptId = attempt.Id;
                logEntry.Attempt = attempt;
            }

            attempt.LogEntries = context.LogEntries;

            attempt.EndTimeUtc = DateTimeOffset.UtcNow;

            attempt.Extracted = extracted;
            attempt.Created = created;
            attempt.Mapped = mapped;
            attempt.Updated = updated;
            attempt.Skipped = skipped;
            attempt.Errored = errored;

            if (attempt.Success)
            {
                var level = extracted == 0 || errored > 0
                    ? ScraperAttemptLogLevel.Warning
                    : ScraperAttemptLogLevel.Information;
                var outcome = extracted == 0
                    ? "Scraper completed with no events extracted"
                    : "Scraper completed";

                context.AddLog(level,
                    $"{outcome}. Extracted={extracted}, mapped={mapped}, created={created}, updated={updated}, skipped={skipped}, errored={errored}.");
            }

            var finalizationToken = cancellationToken.IsCancellationRequested
                ? CancellationToken.None
                : cancellationToken;

            await scrapers.UpdateAsync(scraper, finalizationToken);
            await attemptHistories.UpdateAsync(attempt, finalizationToken);
        }

        return new IngestResult
        {
            Attempt = attempt,
            Success = attempt.Success,
            Errors = errors,

            // TODO: these need to be DTOs...
            //Raw = rawEntities,
            //Mapped = mappedEntities,

            MappingSummary = summaries
        };
    }

    private async Task<(Event? mappedEvent, string result, string message)> ProcessSingleEvent(
        ScraperDefinition scraper,
        Guid attemptId,
        ScrapedEventRaw rawEvent,
        int trackedYear)
    {
        var mappedEvent = mapper.MapEvent(scraper, rawEvent, scraper.UseYearTracking ? trackedYear : null);

        if (mappedEvent.StartUtc == default) return (mappedEvent, "skipped", "Skipped - Missing start date");

        classifier.ClassifyEvent(scraper, rawEvent, mappedEvent);

        var existingEvent = await events.Find(mappedEvent);

        if (existingEvent is not null)
        {
            var revisions = ReviseEventFields(existingEvent, mappedEvent);

            if (revisions is { Count: > 0 })
            {
                existingEvent.UpdatedAtUtc = DateTimeOffset.UtcNow;

                rawEvent.EventId = existingEvent.Id;
                rawEvent.Outcome = ScrapedEventOutcome.Updated;

                await events.UpdateAsync(existingEvent);

                await eventRevisions.AddAsync(new EventRevision
                {
                    Id = Guid.NewGuid(),
                    EventId = existingEvent.Id,
                    AttemptHistoryId = attemptId,
                    RawEventId = rawEvent.Id,
                    Reason = EventRevisionReason.FieldUpdate,
                    ChangedAtUtc = DateTimeOffset.UtcNow,
                    ChangedFieldsJson = JsonSerializer.Serialize(revisions)
                });

                return (mappedEvent, "updated", $"Updated - [{existingEvent.Title}]({existingEvent.Id})");
            }

            rawEvent.Outcome = ScrapedEventOutcome.Skipped;

            return (mappedEvent, "skipped", $"Skipped - No changes - [{existingEvent.Title}]({existingEvent.Id})");
        }
        else
        {
            rawEvent.EventId = mappedEvent.Id;
            rawEvent.Outcome = ScrapedEventOutcome.Created;

            await events.AddAsync(mappedEvent);

            return (mappedEvent, "created", $"Created - [{mappedEvent.Title}]({mappedEvent.Id})");
        }
    }

    private static List<RevisionResult> ReviseEventFields(Event current, Event incoming)
    {
        var changes = new List<RevisionResult>();

        current.CategoryId = Update("Category", current.CategoryId, incoming.CategoryId, changes);
        current.Title = Update("Title", current.Title, incoming.Title, changes);
        current.Location = Update("Location", current.Location, incoming.Location, changes);
        current.Description = Update("Description", current.Description, incoming.Description, changes);

        current.StartUtc = Update("StartUtc", current.StartUtc, incoming.StartUtc, changes);
        current.StartPrecision = Update("StartPrecision", current.StartPrecision, incoming.StartPrecision, changes);
        current.EndUtc = Update("EndUtc", current.EndUtc, incoming.EndUtc, changes);
        current.EndPrecision = Update("EndPrecision", current.EndPrecision, incoming.EndPrecision, changes);

        current.MinPrice = Update("MinPrice", current.MinPrice, incoming.MinPrice, changes);
        current.MaxPrice = Update("MaxPrice", current.MaxPrice, incoming.MaxPrice, changes);

        current.DetailsUrl = Update("DetailsUrl", current.DetailsUrl, incoming.DetailsUrl, changes);
        current.ImageUrl = Update("ImageUrl", current.ImageUrl, incoming.ImageUrl, changes);
        current.TicketUrl = Update("TicketUrl", current.TicketUrl, incoming.TicketUrl, changes);

        return changes;
    }

    private static T Update<T>(
        string field,
        T current,
        T incoming,
        List<RevisionResult> changes)
    {
        var type = typeof(T);
        bool isDifferent = !EqualityComparer<T>.Default.Equals(current, incoming);

        bool isNotDefault = !EqualityComparer<T>.Default.Equals(incoming, default);
        bool shouldTrustDefault = type.IsEnum || (type.IsValueType && Nullable.GetUnderlyingType(type) == null);

        if (isDifferent && (isNotDefault || shouldTrustDefault))
        {
            changes.Add(new RevisionResult(
                Updated: true,
                Field: field,
                OldValue: current?.ToString() ?? "(null)",
                NewValue: incoming?.ToString() ?? "(null)"
            ));

            return incoming;
        }

        return current;
    }
}