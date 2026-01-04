using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Core.Interfaces.Repositories;
using Tendril.Engine.Abstractions;
using Tendril.Engine.Models;

namespace Tendril.Engine.Runtime;

public class IngestionService(
    ILogger<IngestionService> logger,
    IAttemptHistoryRepository attemptHistories,
    IEventRepository events,
    IRawEventRepository rawEvents,
    IScraperRepository scrapers,
    IEventMapper mapper,
    IScrapeExecutor executor) : IIngestionService
{
    public async Task<IngestResult> Ingest(ScraperDefinition scraper, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting ingestion for {Scraper}", scraper.Name);

        var start = DateTimeOffset.UtcNow;
        int created = 0, updated = 0, extracted = 0, errored = 0, skipped = 0;

        // 1. Create Attempt Record IMMEDIATELY (Mark as Running)
        var attempt = new ScraperAttemptHistory
        {
            Id = Guid.NewGuid(),
            ScraperDefinitionId = scraper.Id,
            StartTimeUtc = start,
            Success = false, // Will set to true at end
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
                    RawDataJson = System.Text.Json.JsonSerializer.Serialize(raw)
                };

                await rawEvents.AddAsync(rawEntity, cancellationToken);

                rawEntities.Add(rawEntity);

                // B. Map & Upsert Event
                try
                {
                    var (mappedEvent, status, summary) = await ProcessSingleEventAsync(scraper, rawEntity);

                    if (mappedEvent is not null)
                    {
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

    private async Task<(Event? mappedEvent, string result, string message)> ProcessSingleEventAsync(ScraperDefinition scraper, ScrapedEventRaw rawEntity)
    {
        var mappedEvent = mapper.Map(scraper, rawEntity);

        if (mappedEvent.StartUtc == default) return (mappedEvent, "skipped", "Skipped - Missing start date");

        var existingEvent = await events.Find(mappedEvent);

        if (existingEvent is not null)
        {
            var updated = false;

            UpdateEventFields(existingEvent, mappedEvent, ref updated);

            if (updated)
            {
                existingEvent.UpdatedAtUtc = DateTimeOffset.UtcNow;
                rawEntity.EventId = existingEvent.Id;

                await events.UpdateAsync(existingEvent);

                return (mappedEvent, "updated", $"Updated - [{existingEvent.Title}]({existingEvent.Id})");
            }

            return (mappedEvent, "skipped", $"Skipped - No changes - [{existingEvent.Title}]({existingEvent.Id})");
        }
        else
        {
            await events.AddAsync(mappedEvent);

            rawEntity.EventId = mappedEvent.Id;

            return (mappedEvent, "created", $"Created - [{mappedEvent.Title}]({mappedEvent.Id})");
        }
    }

    private static void UpdateEventFields(Event current, Event incoming, ref bool updated)
    {
        current.Title = UpdateIfChanged(current.Title, incoming.Title, ref updated);
        current.Location = UpdateIfChanged(current.Location, incoming.Location, ref updated);
        current.Description = UpdateIfChanged(current.Description, incoming.Description, ref updated);

        current.StartUtc = UpdateIfChanged(current.StartUtc, incoming.StartUtc, ref updated);
        current.EndUtc = UpdateIfChanged(current.EndUtc, incoming.EndUtc, ref updated);

        current.MinPrice = UpdateIfChanged(current.MinPrice, incoming.MinPrice, ref updated);
        current.MaxPrice = UpdateIfChanged(current.MaxPrice, incoming.MaxPrice, ref updated);

        current.ImageUrl = UpdateIfChanged(current.ImageUrl, incoming.ImageUrl, ref updated);
        current.DetailsUrl = UpdateIfChanged(current.DetailsUrl, incoming.DetailsUrl, ref updated);
        current.TicketUrl = UpdateIfChanged(current.TicketUrl, incoming.TicketUrl, ref updated);
    }

    private static T UpdateIfChanged<T>(T current, T incoming, ref bool updated)
    {
        if (!EqualityComparer<T>.Default.Equals(current, incoming) &&
            !EqualityComparer<T>.Default.Equals(incoming, default))
        {
            updated = true;

            return incoming;
        }

        return current;
    }

    record UpdateResult(bool Updated, string Field, string? OldValue, string? NewValue);
}