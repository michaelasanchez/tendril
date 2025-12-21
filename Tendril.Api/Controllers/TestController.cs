using Microsoft.AspNetCore.Mvc;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;
using Tendril.Engine.Abstractions;

namespace Tendril.Api.Controllers;

[ApiController]
[Route("api/scrapers/{scraperId:guid}/runs")]
public class ScraperRunsController(
    IScraperRepository scrapers,
    IAttemptHistoryRepository attempts,
    IRawEventRepository rawEvents,
    IEventRepository events,
    IScrapeExecutor executor,
    IEventMapper mapper,
    IIngestionService ingestionService) : ControllerBase
{
    private readonly IIngestionService _service;

    // 1️⃣ Test selectors only (no DB writes, no mapping)
    [HttpPost("test-selectors")]
    public async Task<ActionResult> TestSelectors(Guid scraperId)
    {
        var scraper = await scrapers.GetByIdWithDetailsAsync(scraperId);

        if (scraper == null)
            return NotFound();

        var result = await executor.RunScraperAsync(scraper, selectorsOnly: true);

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
        var scraper = await scrapers.GetByIdWithDetailsAsync(scraperId);

        if (scraper == null)
            return NotFound();

        var raw = await rawEvents.GetMostRecentForScraperAsync(scraperId);

        if (raw == null)
            return BadRequest("No raw events available to test mapping.");

        var mapped = mapper.Map(scraper, raw);

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
        var scraper = await scrapers.GetByIdWithDetailsAsync(scraperId);
        if (scraper == null)
            return NotFound();

        var result = await executor.RunScraperAsync(scraper);

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

            var mappedEvent = mapper.Map(scraper, rawEntity);

            mapped.Add(mappedEvent);
        }

        return Ok(new
        {
            result.Success,
            result.ErrorMessage,
            Raw = result.RawEvents?.Count ?? 0,
            Count = mapped?.Count ?? 0
        });
    }

    // 4️⃣ Production run (writes raw + mapped events, same logic as the worker)
    [HttpPost("run-now")]
    public async Task<ActionResult> RunNow(Guid scraperId)
    {
        var scraper = await scrapers.GetByIdWithDetailsAsync(scraperId);

        if (scraper == null)
            return NotFound();

        var result = await ingestionService.Ingest(scraper);

        return Ok(result);
    }
}
