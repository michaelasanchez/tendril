using Microsoft.AspNetCore.Mvc;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Core.Interfaces.Repositories;
using Tendril.Engine.Abstractions;

namespace Tendril.Api.Controllers;

[ApiController]
[Route("api/scrapers/{scraperId:guid}/runs")]
public class ScraperRunsController : ControllerBase
{
    private readonly IScraperRepository _scrapers;
    private readonly IAttemptHistoryRepository _attempts;
    private readonly IRawEventRepository _rawEvents;
    private readonly IEventRepository _events;
    private readonly IScrapeExecutor _executor;
    private readonly IEventMapper _mapper;

    public ScraperRunsController(
        IScraperRepository scrapers,
        IAttemptHistoryRepository attempts,
        IRawEventRepository rawEvents,
        IEventRepository events,
        IScrapeExecutor executor,
        IEventMapper mapper)
    {
        _scrapers = scrapers;
        _attempts = attempts;
        _rawEvents = rawEvents;
        _events = events;
        _executor = executor;
        _mapper = mapper;
    }

    // 1️⃣ Test selectors only (no DB writes, no mapping)
    [HttpPost("test-selectors")]
    public async Task<ActionResult> TestSelectors(Guid scraperId)
    {
        var scraper = await _scrapers.GetByIdWithDetailsAsync(scraperId);

        if (scraper == null)
            return NotFound();

        var result = await _executor.RunScraperAsync(scraper, selectorsOnly: true);

        return Ok(new
        {
            success = result.Success,
            error = result.ErrorMessage,
            raw = result.RawEvents
        });
    }

    // 2️⃣ Test mapping only (takes most recent raw event)
    [HttpPost("test-mapping")]
    public async Task<ActionResult> TestMapping(Guid scraperId)
    {
        var scraper = await _scrapers.GetByIdWithDetailsAsync(scraperId);
        if (scraper == null)
            return NotFound();

        var raw = await _rawEvents.GetMostRecentForScraperAsync(scraperId);
        if (raw == null)
            return BadRequest("No raw events available to test mapping.");

        var mapped = _mapper.Map(scraper, raw);

        return Ok(new
        {
            raw = raw.RawDataJson,
            mapped
        });
    }

    // 3️⃣ Full end-to-end test run (does NOT persist anything)
    [HttpPost("test-run")]
    public async Task<ActionResult> TestRun(Guid scraperId)
    {
        var scraper = await _scrapers.GetByIdWithDetailsAsync(scraperId);
        if (scraper == null)
            return NotFound();

        var result = await _executor.RunScraperAsync(scraper);

        var mapped = new List<object>();

        foreach (var raw in result.RawEvents)
        {
            var rawEntity = new ScrapedEventRaw
            {
                Id = Guid.NewGuid(),
                ScraperDefinitionId = scraper.Id,
                ScrapedAtUtc = DateTimeOffset.UtcNow,
                RawDataJson = System.Text.Json.JsonSerializer.Serialize(raw)
            };

            var mappedEvent = _mapper.Map(scraper, rawEntity);
            mapped.Add(mappedEvent);
        }

        return Ok(new
        {
            result.Success,
            result.ErrorMessage,
            raw = result.RawEvents,
            mapped
        });
    }

    // 4️⃣ Production run (writes raw + mapped events, same logic as the worker)
    [HttpPost("run-now")]
    public async Task<ActionResult> RunNow(Guid scraperId)
    {
        var scraper = await _scrapers.GetByIdWithDetailsAsync(scraperId);
        if (scraper == null)
            return NotFound();

        var start = DateTimeOffset.UtcNow;
        var result = await _executor.RunScraperAsync(scraper);
        var end = DateTimeOffset.UtcNow;

        // record attempt
        var attempt = new ScraperAttemptHistory
        {
            Id = Guid.NewGuid(),
            ScraperDefinitionId = scraper.Id,
            StartTimeUtc = start,
            EndTimeUtc = end,
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };

        await _attempts.AddAsync(attempt);

        if (!result.Success)
        {
            scraper.LastFailureUtc = end;
            scraper.State = ScraperState.Unhealthy;
            await _scrapers.UpdateAsync(scraper);

            return Ok(new
            {
                success = false,
                error = result.ErrorMessage
            });
        }

        scraper.LastSuccessUtc = end;
        scraper.State = ScraperState.Healthy;

        var mapped = new List<object>();
        var inserted = 0;

        foreach (var raw in result.RawEvents)
        {
            // save raw
            var rawEntity = new ScrapedEventRaw
            {
                Id = Guid.NewGuid(),
                ScraperDefinitionId = scraper.Id,
                RawDataJson = System.Text.Json.JsonSerializer.Serialize(raw),
                ScrapedAtUtc = end
            };

            await _rawEvents.AddAsync(rawEntity);

            // map and save event
            var mappedEvent = _mapper.Map(scraper, rawEntity);

            var existing = await _events.Find(mappedEvent);

            if (existing is not null)
            {
                if (existing.StartUtc != mappedEvent.StartUtc && mappedEvent.StartUtc != default)
                {
                    existing.StartUtc = mappedEvent.StartUtc;
                    existing.UpdatedAtUtc = DateTimeOffset.UtcNow;
                }
            }
            else
            {
                await _events.AddAsync(mappedEvent);

                inserted++;
            }

            mapped.Add(mappedEvent);
        }

        await _scrapers.UpdateAsync(scraper);

        return Ok(new
        {
            success = true,
            raw = result.RawEvents,
            inserted,
            mapped
        });
    }
}
