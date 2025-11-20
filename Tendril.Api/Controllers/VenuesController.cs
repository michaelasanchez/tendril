using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Api.Controllers;

[ApiController]
[Route("api/venues")]
public class VenuesController : ControllerBase
{
    private readonly IVenueRepository _venues;
    private readonly IMapper _mapper;

    public VenuesController(IVenueRepository venues, IMapper mapper)
    {
        _venues = venues;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VenueDto>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _venues.GetAllAsync(cancellationToken);
        return Ok(_mapper.Map<IEnumerable<VenueDto>>(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VenueDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var venue = await _venues.GetByIdAsync(id, cancellationToken);
        if (venue is null) return NotFound();

        return Ok(_mapper.Map<VenueDto>(venue));
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

        await _venues.AddAsync(venue, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = venue.Id }, _mapper.Map<VenueDto>(venue));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, VenueDto request, CancellationToken cancellationToken)
    {
        var venue = await _venues.GetByIdAsync(id, cancellationToken);
        if (venue is null) return NotFound();

        venue.Name = request.Name;
        venue.Address = request.Address;
        venue.Website = request.Website;

        await _venues.UpdateAsync(venue, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var venue = await _venues.GetByIdAsync(id, cancellationToken);
        if (venue is null) return NotFound();

        await _venues.DeleteAsync(venue, cancellationToken);
        return NoContent();
    }
}
