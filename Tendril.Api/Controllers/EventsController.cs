using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
using Tendril.Core.Domain.Enums;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController(IEventRepository events, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetAll(
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate,
        CancellationToken cancellationToken)
    {
        var list = await events.GetAllAsync(startDate ?? DateTime.Today, endDate, cancellationToken);

        return Ok(mapper.Map<IEnumerable<EventDto>>(list));
    }

    [HttpGet("{scraperId:guid}")]
    public async Task<ActionResult<VenueDto>> GetByScraperId(
        [FromRoute] Guid scraperId,
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate,
        CancellationToken cancellationToken)
    {
        var list = await events.GetByScraperIdAsync(scraperId, startDate, endDate, cancellationToken);

        return Ok(mapper.Map<IEnumerable<EventDto>>(list));
    }

    [HttpPatch("{eventId:guid}")]
    public async Task<ActionResult> UpdateEvent(
        [FromRoute] Guid eventId,
        [FromBody] PatchEventRequest request,
        CancellationToken ct)
    {
        var @event = await events.GetById(eventId, ct);

        if (@event is not null)
        {
            if (request.Category is string category)
            {
                @event.Category = category;
            }

            if (request.Status is EventStatus status)
            {
                @event.Status = status;
                @event.StatusAtUtc = DateTimeOffset.UtcNow;
            }

            await events.UpdateAsync(@event, ct);
        }

        return NoContent();
    }
}
