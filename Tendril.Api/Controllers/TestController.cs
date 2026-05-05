using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;
using Tendril.Engine.Abstractions;
using Tendril.Engine.Models;
using Tendril.Engine.Utils;

namespace Tendril.Api.Controllers;

[ApiController]
[Route("scrapers/{scraperId:guid}/runs")]
[Authorize]
public class ScraperRunsController(
    IScraperRepository scrapers,
    IRawEventRepository rawEvents,
    IScrapeExecutor executor,
    IMapperService mapper,
    IIngestionService ingestionService) : ControllerBase
{
    // 1) Test selectors only (Stream -> List in memory)
    [HttpPost("test-actions")]
    public async Task<ActionResult> TestActions(Guid scraperId, [FromQuery] int? limit, CancellationToken ct)
    {
        var scraper = await scrapers.GetByIdWithDetailsAsync(scraperId, ct);
        if (scraper == null) return NotFound();

        limit ??= 10;
        var events = new List<RawScrapedData>();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        try
        {
            await foreach (var item in executor.RunScraperAsync(scraper, cts.Token))
            {
                events.Add(item);
                if (events.Count >= limit)
                {
                    cts.Cancel(); // Signal the scraper to stop immediately
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { }
        catch (Exception ex)
        {
            return Ok(new
            {
                success = false,
                error = ex.Message,
                count = events.Count,
                raw = events
            });
        }

        // This handles BOTH the normal completion AND the limit-reached completion
        return Ok(new
        {
            success = true,
            error = (string?)null,
            count = events.Count,
            raw = events
        });
    }

    // 2️) Test mapping only (No changes needed, uses DB)
    [HttpPost("test-mapping")]
    public async Task<ActionResult> TestMapping(Guid scraperId, [FromQuery] int? limit, CancellationToken ct)
    {
        var yearTracker = new YearTracker();

        var scraper = await scrapers.GetByIdWithDetailsAsync(scraperId, ct);
        if (scraper == null) return NotFound();

        var raw = await rawEvents.GetMostRecentForScraperAsync(scraperId, ct);

        if (raw == null)
            return BadRequest("No raw events available to test mapping.");

        var mapped = mapper.MapEvent(scraper, raw, yearTracker.CurrentYear);

        if (mapped?.StartUtc is not null)
        {
            int assignedYear = yearTracker.ProcessMonth(mapped.StartUtc.Month);

            if (scraper.UseYearTracking && assignedYear != mapped.StartUtc.Year)
            {
                // If the tracker bumped the year, update the event objects
                int diff = assignedYear - mapped.StartUtc.Year;
                mapped.StartUtc = mapped.StartUtc.AddYears(diff);

                if (mapped.EndUtc is not null)
                    mapped.EndUtc = mapped.EndUtc.Value.AddYears(diff);
            }
        }

        return Ok(new
        {
            raw = raw.RawDataJson,
            mapped
        });
    }

    // 3️) Full end-to-end test run (Stream -> Map -> Return JSON, no DB save)
    [HttpPost("test-run")]
    public async Task<ActionResult> TestRun(Guid scraperId, CancellationToken ct)
    {
        var yearTracker = new YearTracker();

        var scraper = await scrapers.GetByIdWithDetailsAsync(scraperId, ct);
        if (scraper == null)
            return NotFound();

        var mappedEvents = new List<object>();
        var rawEvents = new List<object>();

        try
        {
            await foreach (var raw in executor.RunScraperAsync(scraper, ct))
            {
                rawEvents.Add(raw);

                // Simulate the Entity wrapper required by the Mapper
                var rawEntity = new ScrapedEventRaw
                {
                    Id = Guid.NewGuid(),
                    ScraperDefinitionId = scraper.Id,
                    ScrapedAtUtc = DateTimeOffset.UtcNow,
                    // The mapper expects the serialized JSON string
                    RawDataJson = System.Text.Json.JsonSerializer.Serialize(raw)
                };

                var mapped = mapper.MapEvent(scraper, rawEntity, yearTracker.CurrentYear);

                if (mapped?.StartUtc is not null)
                {
                    int assignedYear = yearTracker.ProcessMonth(mapped.StartUtc.Month);

                    if (scraper.UseYearTracking && assignedYear != mapped.StartUtc.Year)
                    {
                        // If the tracker bumped the year, update the event objects
                        int diff = assignedYear - mapped.StartUtc.Year;
                        mapped.StartUtc = mapped.StartUtc.AddYears(diff);

                        if (mapped.EndUtc is not null)
                            mapped.EndUtc = mapped.EndUtc.Value.AddYears(diff);
                    }
                }

                if (mapped is not null)
                {
                    mappedEvents.Add(mapped);
                }
            }

            return Ok(new
            {
                success = true,
                error = (string?)null,
                raw = rawEvents,
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
                raw = rawEvents,
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