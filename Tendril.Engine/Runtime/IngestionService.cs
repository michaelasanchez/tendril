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
    IMapperService mapper,
    IClassificationService classifier,
    IScrapeExecutor executor) : IIngestionService
{
    public async Task<IngestResult> Ingest(ScraperDefinition scraper, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting ingestion for {Scraper}", scraper.Name);

        var start = DateTimeOffset.UtcNow;
        int created = 0, updated = 0, extracted = 0, errored = 0, skipped = 0;

        var yearTracker = new YearTracker(DateTimeOffset.UtcNow);

        // 1. Create Attempt Record IMMEDIATELY (Mark as Running)
        var attempt = new ScraperAttemptHistory
        {
            Id = Guid.NewGuid(),
            ScraperDefinitionId = scraper.Id,
            StartTimeUtc = start,
            Success = false,
            ErrorMessage = "Running..."
        };

        await attemptHistories.Add(attempt, cancellationToken);

        var errors = new List<string>();

        var rawEntities = new List<ScrapedEventRaw>();
        var mappedEntities = new List<Event>();
        var summaries = new List<string>();

        try
        {
            // 2. Consume the Stream (Process 1 item at a time)
            await foreach (var raw in executor.RunScraperAsync(scraper, cancellationToken))
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
                    var (mappedEvent, status, summary) = await ProcessSingleEventAsync(
                        scraper,
                        attempt.Id,
                        rawEntity,
                        yearTracker.CurrentYear);

                    if (mappedEvent?.StartUtc is not null && mappedEvent.StartUtc != default)
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

                    if (mappedEvent is not null)
                    {
                        if (scraper.RequireReview)
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
                    errors.Add(ex.Message);

                    errored++;

                    logger.LogError(ex, "Error mapping event");
                }
            }

            // 3. Success Completion
            scraper.State = ScraperState.Healthy;
            scraper.LastSuccessUtc = DateTimeOffset.UtcNow;
            attempt.Success = true;
            attempt.ErrorMessage = null;
        }
        catch (Exception ex)
        {
            // 4. Failure Completion
            logger.LogError(ex, "Scraper job failed");
            scraper.State = ScraperState.Unhealthy;
            scraper.LastFailureUtc = DateTimeOffset.UtcNow;
            scraper.LastErrorMessage = ex.Message;

            attempt.Success = false;
            attempt.ErrorMessage = ex.Message;
        }
        finally
        {
            // 5. Finalize Stats
            attempt.EndTimeUtc = DateTimeOffset.UtcNow;
            attempt.Extracted = extracted;
            attempt.Created = created;
            attempt.Updated = updated;
            attempt.Skipped = skipped;
            attempt.Errored = errored;

            await scrapers.UpdateAsync(scraper, cancellationToken);
            await attemptHistories.UpdateAsync(attempt, cancellationToken);
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

    private async Task<(Event? mappedEvent, string result, string message)> ProcessSingleEventAsync(
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

            return (mappedEvent, "skipped", $"Skipped - No changes - [{existingEvent.Title}]({existingEvent.Id})");
        }
        else
        {
            rawEvent.EventId = mappedEvent.Id;

            await events.AddAsync(mappedEvent);

            return (mappedEvent, "created", $"Created - [{mappedEvent.Title}]({mappedEvent.Id})");
        }
    }

    private static List<RevisionResult> ReviseEventFields(Event current, Event incoming)
    {
        var changes = new List<RevisionResult>();

        current.Category = Update("Category", current.Category, incoming.Category, changes);
        current.Title = Update("Title", current.Title, incoming.Title, changes);
        current.Location = Update("Location", current.Location, incoming.Location, changes);
        current.Description = Update("Description", current.Description, incoming.Description, changes);

        current.StartUtc = Update("StartUtc", current.StartUtc, incoming.StartUtc, changes);
        current.EndUtc = Update("EndUtc", current.EndUtc, incoming.EndUtc, changes);

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
        if (!EqualityComparer<T>.Default.Equals(current, incoming) &&
            !EqualityComparer<T>.Default.Equals(incoming, default))
        {
            changes.Add(new RevisionResult(
                Updated: true,
                Field: field,
                OldValue: current?.ToString(),
                NewValue: incoming?.ToString()
            ));

            return incoming;
        }

        return current;
    }
}