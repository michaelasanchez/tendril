using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
using Tendril.Core.Domain;
using Tendril.Core.Domain.Enums;
using Tendril.Core.Interfaces.Repositories;
using Tendril.Data.Models;

namespace Tendril.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController(IEventRepository events, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<EventDto>>> GetAll(
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate,
        [FromQuery] string? title,
        [FromQuery(Name = "category")] List<string>? categories,
        [FromQuery(Name = "venue")] List<Guid>? venueIds,
        [FromQuery] int? limit,
        [FromQuery] Guid? cursor,
        CancellationToken cancellationToken)
    {
        var filter = new EventFilter
        {
            StartDate = startDate ?? DateTime.Today,
            EndDate = endDate,
            Title = title,
            Categories = categories,
            VenueIds = venueIds
        };

        var result = await events.GetAllAsync(filter, limit, cursor, cancellationToken);

        return Ok(new PagedResponse<EventDto>
        {
            Items = mapper.Map<List<EventDto>>(result.Items),
            NextCursor = result.NextCursor,
            HasNextPage = result.HasNextPage
        });
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
