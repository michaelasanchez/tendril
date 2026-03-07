using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Api.Controllers;

[ApiController]
[Route("scrapers")]
[Authorize]
public class ScrapersController(
    IScraperRepository scrapers,
    IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScraperDto>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await scrapers.GetAllAsync(cancellationToken);

        return Ok(mapper.Map<IEnumerable<ScraperDto>>(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ScraperDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var scraper = await scrapers.GetByIdWithDisabledDetailsAsync(id, cancellationToken);

        if (scraper is null) return NotFound();

        return Ok(mapper.Map<ScraperDto>(scraper));
    }

    [HttpPost]
    public async Task<ActionResult<ScraperDto>> Create(CreateScraperRequest request, CancellationToken cancellationToken)
    {
        var scraper = new ScraperDefinition
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            BaseUrl = request.BaseUrl,
            Disabled = request.Disabled,
            Notes = request.Notes ?? string.Empty,
            ExecutionMode = request.ExecutionMode ?? Core.Domain.Enums.ExecutionMode.Dynamic,
            ExtractionStrategy = request.ExtractionStrategy ?? Core.Domain.Enums.ExtractionStrategy.Css,
            PaginationType = request.PaginationType ?? Core.Domain.Enums.PaginationType.None,
            UseReferenceYear = request.UseReferenceYear,
            VenueId = request.VenueId
        };

        await scrapers.AddAsync(scraper, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = scraper.Id },
            mapper.Map<ScraperDto>(scraper));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateScraperRequest request, CancellationToken cancellationToken)
    {
        var scraper = await scrapers.GetByIdAsync(id, cancellationToken);
        if (scraper is null) return NotFound();

        if (request.Name is not null) scraper.Name = request.Name;
        if (request.BaseUrl is not null) scraper.BaseUrl = request.BaseUrl;
        if (request.Disabled is not null) scraper.Disabled = request.Disabled.Value;
        if (request.Notes is not null) scraper.Notes = request.Notes;
        if (request.ExecutionMode is not null) scraper.ExecutionMode = request.ExecutionMode.Value;
        if (request.ExtractionStrategy is not null) scraper.ExtractionStrategy = request.ExtractionStrategy.Value;
        if (request.PaginationType is not null) scraper.PaginationType = request.PaginationType.Value;
        if (request.UseReferenceYear is not null) scraper.UseReferenceYear = request.UseReferenceYear.Value;
        if (request.VenueId is not null) scraper.VenueId = request.VenueId;

        await scrapers.UpdateAsync(scraper, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var scraper = await scrapers.GetByIdAsync(id, cancellationToken);
        if (scraper is null) return NotFound();

        await scrapers.DeleteAsync(scraper, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/summaries")]
    public async Task<ActionResult> Summarize(Guid id, CancellationToken cancellationToken)
    {
        var summary = await scrapers.GetSummaryByIdAsync(id, cancellationToken);
        if (summary is null) return NotFound();

        return Ok(summary);
    }
}
