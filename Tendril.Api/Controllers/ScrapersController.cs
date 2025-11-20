using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;
using Tendril.Engine.Abstractions;

namespace Tendril.Api.Controllers;

[ApiController]
[Route("api/scrapers")]
public class ScrapersController : ControllerBase
{
    private readonly IScraperRepository _scrapers;
    private readonly IMapper _mapper;
    private readonly IScrapeExecutor _executor;

    public ScrapersController(
        IScraperRepository scrapers,
        IMapper mapper,
        IScrapeExecutor executor)
    {
        _scrapers = scrapers;
        _mapper = mapper;
        _executor = executor;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScraperDto>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _scrapers.GetAllAsync(cancellationToken);
        return Ok(_mapper.Map<IEnumerable<ScraperDto>>(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ScraperDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var scraper = await _scrapers.GetByIdWithDetailsAsync(id, cancellationToken);
        if (scraper is null) return NotFound();

        return Ok(_mapper.Map<ScraperDto>(scraper));
    }

    [HttpPost]
    public async Task<ActionResult<ScraperDto>> Create(CreateScraperRequest request, CancellationToken cancellationToken)
    {
        var scraper = new ScraperDefinition
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            BaseUrl = request.BaseUrl,
            IsDynamic = request.IsDynamic,
            VenueId = request.VenueId
        };

        await _scrapers.AddAsync(scraper, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = scraper.Id },
            _mapper.Map<ScraperDto>(scraper));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateScraperRequest request, CancellationToken cancellationToken)
    {
        var scraper = await _scrapers.GetByIdAsync(id, cancellationToken);
        if (scraper is null) return NotFound();

        if (request.Name != null) scraper.Name = request.Name;
        if (request.BaseUrl != null) scraper.BaseUrl = request.BaseUrl;
        if (request.IsDynamic.HasValue) scraper.IsDynamic = request.IsDynamic.Value;
        if (request.VenueId.HasValue) scraper.VenueId = request.VenueId;

        await _scrapers.UpdateAsync(scraper, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var scraper = await _scrapers.GetByIdAsync(id, cancellationToken);
        if (scraper is null) return NotFound();

        await _scrapers.DeleteAsync(scraper, cancellationToken);
        return NoContent();
    }

    // ⭐ TEST RUN ⭐
    [HttpPost("{id:guid}/test-run")]
    public async Task<ActionResult<TestRunResultDto>> TestRun(Guid id, CancellationToken cancellationToken)
    {
        var scraper = await _scrapers.GetByIdWithDetailsAsync(id, cancellationToken);
        if (scraper is null)
            return NotFound();

        var result = await _executor.RunScraperAsync(scraper, cancellationToken);

        return Ok(new TestRunResultDto(
            result.Success,
            result.ErrorMessage,
            [.. result.RawEvents.Select(e => e.Fields)]
        ));
    }
}
