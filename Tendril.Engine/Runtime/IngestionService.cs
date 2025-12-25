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
        logger.LogInformation("Running scraper {Scraper}", scraper.Name);

        var start = DateTimeOffset.UtcNow;

        var result = await executor.RunScraperAsync(scraper, cancellationToken);

        var end = DateTimeOffset.UtcNow;

        // Log attempt
        var attempt = new ScraperAttemptHistory
        {
            Id = Guid.NewGuid(),
            ScraperDefinitionId = scraper.Id,
            StartTimeUtc = start,
            EndTimeUtc = end,
            Success = result.Success,
            Extracted = result.RawEvents.Count,
            ErrorMessage = result.ErrorMessage
        };

        await attemptHistories.Add(attempt, cancellationToken);

        var scraped = new List<ScrapedEventRaw>();
        var mapped = new List<Event>();
        int created = 0, updated = 0;

        if (result.Success)
        {
            scraper.LastSuccessUtc = end;
            scraper.State = ScraperState.Healthy;

            foreach (var raw in result.RawEvents)
            {
                var rawEntity = new ScrapedEventRaw
                {
                    Id = Guid.NewGuid(),
                    ScraperDefinitionId = scraper.Id,
                    ScrapedAtUtc = end,
                    RawDataJson = System.Text.Json.JsonSerializer.Serialize(raw)
                };

                scraped.Add(rawEntity);

                await rawEvents.AddAsync(rawEntity, cancellationToken);

                // Map to consolidated Event
                try
                {
                    var mappedEvent = mapper.Map(scraper, rawEntity);

                    if (mappedEvent.StartUtc == default)
                    {
                        continue;
                    }

                    mapped.Add(mappedEvent);

                    var existingEvent = await events.Find(mappedEvent);

                    if (existingEvent is not null)
                    {
                        var dirty = false;

                        existingEvent.Title = UpdateIfChanged(existingEvent.Title, mappedEvent.Title, ref dirty);
                        existingEvent.Description = UpdateIfChanged(existingEvent.Description, mappedEvent.Description, ref dirty);
                        existingEvent.StartUtc = UpdateIfChanged(existingEvent.StartUtc, mappedEvent.StartUtc, ref dirty);
                        existingEvent.EndUtc = UpdateIfChanged(existingEvent.EndUtc, mappedEvent.EndUtc, ref dirty);
                        existingEvent.TicketUrl = UpdateIfChanged(existingEvent.TicketUrl, mappedEvent.TicketUrl, ref dirty);
                        existingEvent.ImageUrl = UpdateIfChanged(existingEvent.ImageUrl, mappedEvent.ImageUrl, ref dirty);
                        existingEvent.Category = UpdateIfChanged(existingEvent.Category, mappedEvent.Category, ref dirty);

                        if (dirty)
                        {
                            updated++;

                            existingEvent.UpdatedAtUtc = DateTimeOffset.UtcNow;

                            rawEntity.ScraperAttemptHistoryId = attempt.Id;
                            rawEntity.EventId = existingEvent.Id;
                        }
                    }
                    else
                    {
                        await events.AddAsync(mappedEvent, cancellationToken);

                        created++;

                        rawEntity.ScraperAttemptHistoryId = attempt.Id;
                        rawEntity.EventId = mappedEvent.Id;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to map raw event for scraper {ScraperId}", scraper.Id);
                }
            }

            await scrapers.UpdateAsync(scraper, cancellationToken);
        }
        else
        {
            scraper.LastFailureUtc = end;
            scraper.LastErrorMessage = result.ErrorMessage;
            scraper.State = scraper.State == ScraperState.Healthy
                ? ScraperState.Warning
                : ScraperState.Unhealthy;

            logger.LogWarning(
                "Scraper {Scraper} failed: {Error}",
                scraper.Name,
                result.ErrorMessage);
        }

        await scrapers.UpdateAsync(scraper, cancellationToken);

        attempt.Mapped = mapped.Count;
        attempt.Created = created;
        attempt.Updated = updated;

        await attemptHistories.UpdateAsync(attempt, cancellationToken);

        return new IngestResult
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            Attempt = attempt,
            Scraped = scraped,
            Mapped = mapped
        };
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
