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
        int created = 0, updated = 0, extracted = 0;

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

                // B. Map & Upsert Event
                try
                {
                    var result = await ProcessSingleEventAsync(scraper, rawEntity);

                    if (result == "created") created++;
                    if (result == "updated") updated++;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error mapping event");
                    errors.Add(ex.Message);
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

            await scrapers.UpdateAsync(scraper, cancellationToken);
            await attemptHistories.UpdateAsync(attempt, cancellationToken);
        }

        return new IngestResult
        {
            Success = attempt.Success,
            Attempt = attempt
        };
    }

    private async Task<string> ProcessSingleEventAsync(ScraperDefinition scraper, ScrapedEventRaw rawEntity)
    {
        var mappedEvent = mapper.Map(scraper, rawEntity);

        if (mappedEvent.StartUtc == default) return "skipped";

        var existingEvent = await events.Find(mappedEvent);

        if (existingEvent is not null)
        {
            var dirty = false;
            // (Your existing UpdateIfChanged logic here...)
            UpdateEventFields(existingEvent, mappedEvent, ref dirty);

            if (dirty)
            {
                existingEvent.UpdatedAtUtc = DateTimeOffset.UtcNow;
                rawEntity.EventId = existingEvent.Id;

                // Note: EF Core tracking usually handles the update automatically 
                // when you call SaveChanges, or if your repo has an Update method:
                await events.UpdateAsync(existingEvent);
                return "updated";
            }
            return "skipped";
        }
        else
        {
            await events.AddAsync(mappedEvent);

            rawEntity.EventId = mappedEvent.Id;

            return "created";
        }
    }

    // Helper to keep the main method clean
    private void UpdateEventFields(Event current, Event incoming, ref bool isModified)
    {
        current.Title = UpdateIfChanged(current.Title, incoming.Title, ref isModified);
        //current.Location = UpdateIfChanged(current.Location, incoming.Location, ref isModified);
        current.Description = UpdateIfChanged(current.Description, incoming.Description, ref isModified);

        current.StartUtc = UpdateIfChanged(current.StartUtc, incoming.StartUtc, ref isModified);
        current.EndUtc = UpdateIfChanged(current.EndUtc, incoming.EndUtc, ref isModified);

        current.ImageUrl = UpdateIfChanged(current.ImageUrl, incoming.ImageUrl, ref isModified);
        //current.DetailsUrl = UpdateIfChanged(current.DetailsUrl, incoming.DetailsUrl, ref isModified);
        current.TicketUrl = UpdateIfChanged(current.TicketUrl, incoming.TicketUrl, ref isModified);
    }

    private static T UpdateIfChanged<T>(T current, T incoming, ref bool isModified)
    {
        if (!EqualityComparer<T>.Default.Equals(current, incoming) &&
            !EqualityComparer<T>.Default.Equals(incoming, default))
        {
            isModified = true;

            return incoming;
        }

        return current;
    }
}