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

        var mapped = mapper.Map<ScraperDto>(scraper);

        return Ok(mapped);
    }

    [HttpPost]
    public async Task<ActionResult<ScraperDto>> Create(CreateScraperRequest request, CancellationToken cancellationToken)
    {
        var scraper = new ScraperDefinition
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            BaseUrl = request.BaseUrl,
            IsEventFeed = request.IsEventFeed,
            Disabled = request.Disabled,
            Notes = request.Notes ?? string.Empty,
            HasSuggestions = request.HasSuggestions,
            RequiresReview = request.RequiresReview,
            ExecutionMode = request.ExecutionMode ?? Core.Domain.Enums.ExecutionMode.Dynamic,
            ExtractionStrategy = request.ExtractionStrategy ?? Core.Domain.Enums.ExtractionStrategy.Css,
            PaginationType = request.PaginationType ?? Core.Domain.Enums.PaginationType.None,
            UseYearTracking = request.UseYearTracking,
            UseHeadlessBrowser = request.UseHeadlessBrowser,
            VenueId = request.VenueId,
            Method = request.Method,
            Parameters = request.Parameters?
                .Select(x => new ApiParameter
                {
                    Id = Guid.Empty,
                    Key = x.Key,
                    Template = x.Template,
                    Source = x.Source,
                    Target = x.Target,
                    IsRequired = x.IsRequired
                })
                .ToList() ?? []
        };

        await scrapers.AddAsync(scraper, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = scraper.Id },
            mapper.Map<ScraperDto>(scraper));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateScraperRequest request, CancellationToken cancellationToken)
    {
        // 1. Fetch the existing entity with children loaded
        // Ensure your GetByIdAsync or the underlying query uses .Include(x => x.Parameters)
        var scraper = await scrapers.GetByIdAsync(id, cancellationToken);
        if (scraper is null) return NotFound();

        // Standard properties
        if (request.Name is not null) scraper.Name = request.Name;
        if (request.BaseUrl is not null) scraper.BaseUrl = request.BaseUrl;
        if (request.IsEventFeed is not null) scraper.IsEventFeed = request.IsEventFeed.Value;
        if (request.Disabled is not null) scraper.Disabled = request.Disabled.Value;
        if (request.Notes is not null) scraper.Notes = request.Notes;
        if (request.HasSuggestions is not null) scraper.HasSuggestions = request.HasSuggestions.Value;
        if (request.RequiresReview is not null) scraper.RequiresReview = request.RequiresReview.Value;
        if (request.ExecutionMode is not null) scraper.ExecutionMode = request.ExecutionMode.Value;
        if (request.ExtractionStrategy is not null) scraper.ExtractionStrategy = request.ExtractionStrategy.Value;
        if (request.PaginationType is not null) scraper.PaginationType = request.PaginationType.Value;
        if (request.UseYearTracking is not null) scraper.UseYearTracking = request.UseYearTracking.Value;
        if (request.UseHeadlessBrowser is not null) scraper.UseHeadlessBrowser = request.UseHeadlessBrowser.Value;
        if (request.VenueId is not null) scraper.VenueId = request.VenueId;
        if (request.Method is not null) scraper.Method = request.Method;

        // 2. The Collection Sync Logic
        if (request.Parameters is not null)
        {
            // Identify what's coming in
            var incomingIds = request.Parameters
                .Where(x => x.Id.HasValue && x.Id != Guid.Empty)
                .Select(x => x.Id!.Value)
                .ToList();

            // REMOVE: Delete children that exist in the DB but are missing from the request
            var paramsToRemove = scraper.Parameters
                .Where(p => !incomingIds.Contains(p.Id))
                .ToList();

            foreach (var toRemove in paramsToRemove)
            {
                scraper.Parameters.Remove(toRemove);
            }

            // UPDATE / ADD
            foreach (var pDto in request.Parameters)
            {
                var existingParam = scraper.Parameters.FirstOrDefault(p => p.Id != Guid.Empty && p.Id == pDto.Id);

                if (existingParam != null)
                {
                    // UPDATE: Update existing child properties
                    existingParam.Key = pDto.Key ?? string.Empty;
                    existingParam.Template = pDto.Template ?? string.Empty;
                    existingParam.Source = pDto.Source ?? ApiParameterSource.Parent;
                    existingParam.Target = pDto.Target ?? ApiParameterTarget.Query;
                    existingParam.IsRequired = pDto.IsRequired ?? false;
                }
                else
                {
                    // ADD: New child
                    scraper.Parameters.Add(new ApiParameter
                    {
                        Id = pDto.Id ?? Guid.Empty,
                        Key = pDto.Key ?? string.Empty,
                        Template = pDto.Template ?? string.Empty,
                        Source = pDto.Source ?? ApiParameterSource.Parent,
                        Target = pDto.Target ?? ApiParameterTarget.Query,
                        IsRequired = pDto.IsRequired ?? false
                    });
                }
            }
        }

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
