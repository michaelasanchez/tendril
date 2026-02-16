using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;
using Tendril.Engine.Abstractions;
using Tendril.Engine.Models;

namespace Tendril.Api.Controllers;

[ApiController]
[Route("scrapers/{scraperId:guid}/runs")]
public class ScraperRunsController(
    IScraperRepository scrapers,
    IRawEventRepository rawEvents,
    IScrapeExecutor executor,
    IMapperService mapper,
    IIngestionService ingestionService) : ControllerBase
{
    // 1️⃣ Test selectors only (Stream -> List in memory)
    [HttpPost("test-selectors")]
    public async Task<ActionResult> TestSelectors(Guid scraperId, CancellationToken ct)
    {
        var scraper = await scrapers.GetByIdWithDetailsAsync(scraperId, ct);

        if (scraper == null)
            return NotFound();

        var events = new List<RawScrapedData>();

        try
        {
            // Consume the stream
            await foreach (var item in executor.RunScraperAsync(scraper, ct))
            {
                events.Add(item);
            }

            return Ok(new
            {
                success = true,
                error = (string?)null,
                count = events.Count,
                raw = events
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                success = false,
                error = ex.Message,
                count = events.Count,
                raw = events // Return what we found before it crashed
            });
        }
    }

    // 2️⃣ Test mapping only (No changes needed, uses DB)
    [HttpPost("test-mapping")]
    public async Task<ActionResult> TestMapping(Guid scraperId, CancellationToken ct)
    {
        var scraper = await scrapers.GetByIdWithDetailsAsync(scraperId, ct);

        if (scraper == null)
            return NotFound();

        var raw = await rawEvents.GetMostRecentForScraperAsync(scraperId, ct);

        if (raw == null)
            return BadRequest("No raw events available to test mapping.");

        var mapped = mapper.MapEvent(scraper, raw);

        return Ok(new
        {
            raw = raw.RawDataJson,
            mapped
        });
    }

    // 3️⃣ Full end-to-end test run (Stream -> Map -> Return JSON, no DB save)
    [HttpPost("test-run")]
    public async Task<ActionResult> TestRun(Guid scraperId, CancellationToken ct)
    {
        var scraper = await scrapers.GetByIdWithDetailsAsync(scraperId, ct);
        if (scraper == null)
            return NotFound();

        var mappedEvents = new List<object>();
        var rawCount = 0;

        try
        {
            await foreach (var raw in executor.RunScraperAsync(scraper, ct))
            {
                rawCount++;

                // Simulate the Entity wrapper required by the Mapper
                var rawEntity = new ScrapedEventRaw
                {
                    Id = Guid.NewGuid(),
                    ScraperDefinitionId = scraper.Id,
                    ScrapedAtUtc = DateTimeOffset.UtcNow,
                    // The mapper expects the serialized JSON string
                    RawDataJson = System.Text.Json.JsonSerializer.Serialize(raw)
                };

                var mappedEvent = mapper.MapEvent(scraper, rawEntity);
                mappedEvents.Add(mappedEvent);
            }

            return Ok(new
            {
                success = true,
                error = (string?)null,
                rawCount,
                mappedCount = mappedEvents.Count,
                mapped = mappedEvents
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                success = false,
                error = ex.Message,
                rawCount,
                mappedCount = mappedEvents.Count,
                mapped = mappedEvents
            });
        }
    }

    // 4️⃣ Production run (Delegates to IngestionService which now handles the stream)
    [HttpPost("run-now")]
    public async Task<ActionResult> RunNow(Guid scraperId, CancellationToken ct)
    {
        var scraper = await scrapers.GetByIdWithDetailsAsync(scraperId, ct);

        if (scraper == null)
            return NotFound();

        // The IngestionService now internally handles the Stream loop and DB saving
        var result = await ingestionService.Ingest(scraper, ct);

        return Ok(new IngestResultDto
        {
            Attempt = null,
            Success = result.Success,
            Errors = result.Errors,
            Raw = null,
            Mapped = null,
            MappingSummary = result.MappingSummary
        });
    }
}