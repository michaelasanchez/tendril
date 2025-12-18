using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Core.Interfaces.Repositories;
using Tendril.Engine.Abstractions;
using Tendril.Engine.Models;

namespace Tendril.Engine.Runtime;

public class EventIngestionService(
    ILogger<EventIngestionService> logger,
    IAttemptHistoryRepository attemptHistories,
    IEventRepository events,
    IRawEventRepository rawEvents,
    IScraperRepository scrapers,
    IEventMapper mapper,
    IScrapeExecutor executor) : IEventIngestionService
{
    public async Task<IngestResult> Ingest(ScraperDefinition scraper, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running scraper {Scraper}", scraper.Name);

        var start = DateTimeOffset.UtcNow;

        var result = await executor.RunScraperAsync(scraper, false, cancellationToken);

        var end = DateTimeOffset.UtcNow;

        // Log attempt
        var attempt = new ScraperAttemptHistory
        {
            Id = Guid.NewGuid(),
            ScraperDefinitionId = scraper.Id,
            StartTimeUtc = start,
            EndTimeUtc = end,
            Success = result.Success,
            ExtractedCount = result.RawEvents.Count,
            ErrorMessage = result.ErrorMessage
        };

        await attemptHistories.AddAsync(attempt, cancellationToken);

        var scraped = new List<ScrapedEventRaw>();
        var mapped = new List<Event>();
        var inserted = 0;

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

                    mapped.Add(mappedEvent);

                    var existingEvent = await events.Find(mappedEvent);

                    if (existingEvent is not null)
                    {
                        var updated = false;

                        existingEvent.Title = UpdateIfChanged(existingEvent.Title, mappedEvent.Title, ref updated);
                        existingEvent.Description = UpdateIfChanged(existingEvent.Description, mappedEvent.Description, ref updated);
                        existingEvent.StartUtc = UpdateIfChanged(existingEvent.StartUtc, mappedEvent.StartUtc, ref updated);
                        existingEvent.EndUtc = UpdateIfChanged(existingEvent.EndUtc, mappedEvent.EndUtc, ref updated);
                        existingEvent.TicketUrl = UpdateIfChanged(existingEvent.TicketUrl, mappedEvent.TicketUrl, ref updated);
                        existingEvent.ImageUrl = UpdateIfChanged(existingEvent.ImageUrl, mappedEvent.ImageUrl, ref updated);
                        existingEvent.Category = UpdateIfChanged(existingEvent.Category, mappedEvent.Category, ref updated);

                        if (updated)
                        {
                            existingEvent.UpdatedAtUtc = DateTimeOffset.UtcNow;

                            rawEntity.ScraperAttemptHistoryId = attempt.Id;
                            rawEntity.EventId = existingEvent.Id;
                        }
                    }
                    else
                    {
                        await events.AddAsync(mappedEvent, cancellationToken);

                        inserted++;

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
