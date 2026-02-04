using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Api.Controllers;

[ApiController]
[Route("api/venues")]
public class VenuesController(IVenueRepository venues, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VenueDto>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await venues.GetAllAsync(cancellationToken);
        return Ok(mapper.Map<IEnumerable<VenueDto>>(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VenueDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var venue = await venues.GetByIdAsync(id, cancellationToken);
        if (venue is null) return NotFound();

        return Ok(mapper.Map<VenueDto>(venue));
    }

    [HttpPost]
    public async Task<ActionResult<VenueDto>> Create(VenueDto request, CancellationToken cancellationToken)
    {
        var venue = new Venue
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Address = request.Address,
            Website = request.Website
        };

        await venues.AddAsync(venue, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = venue.Id }, mapper.Map<VenueDto>(venue));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, VenueDto request, CancellationToken cancellationToken)
    {
        var venue = await venues.GetByIdAsync(id, cancellationToken);
        if (venue is null) return NotFound();

        venue.Name = request.Name;
        venue.Address = request.Address;
        venue.Website = request.Website;

        await venues.UpdateAsync(venue, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var venue = await venues.GetByIdAsync(id, cancellationToken);
        if (venue is null) return NotFound();

        await venues.DeleteAsync(venue, cancellationToken);
        return NoContent();
    }
}
