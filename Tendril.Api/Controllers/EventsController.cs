using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
using Tendril.Core.Domain.Enums;
using Tendril.Core.Interfaces.Repositories;
using Tendril.Data.Models;

namespace Tendril.Api.Controllers;

[ApiController]
[Route("events")]
public class EventsController(IEventRepository events, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<EventResponse>> Search(
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate,
        [FromQuery] string? title,
        [FromQuery(Name = "category")] List<Guid>? categoryIds,
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
            CategoryIds = categoryIds,
            VenueIds = venueIds
        };

        var result = await events.GetAllAsync(filter, limit, cursor, cancellationToken);

        return Ok(new EventResponse
        {
            Items = mapper.Map<List<EventDto>>(result.Items),
            CategoryIds = [.. result.Items
                .Where(x => x.CategoryId is not null)
                .Select(x => x.CategoryId!.Value)
                .Distinct()],
            VenueIds = [.. result.Items
                .Where(x => x.VenueId is not null)
                .Select(x => x.VenueId!.Value)
                .Distinct()],
            NextCursor = result.NextCursor,
            HasNextPage = result.HasNextPage,
            TotalCount = result.TotalCount
        });
    }

    [HttpGet("{eventId:guid}")]
    public async Task<ActionResult<VenueDto>> GetById(
        [FromRoute] Guid eventId,
        CancellationToken cancellationToken)
    {
        var @event = await events.GetById(eventId, cancellationToken);

        return Ok(mapper.Map<EventDto>(@event));
    }

    [HttpGet("scraper/{scraperId:guid}")]
    public async Task<ActionResult<VenueDto>> GetByScraperId(
        [FromRoute] Guid scraperId,
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate,
        CancellationToken cancellationToken)
    {
        var list = await events.GetByScraperIdAsync(scraperId, startDate, endDate, cancellationToken);

        return Ok(mapper.Map<IEnumerable<EventDto>>(list));
    }

    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<PendingEventReviewDto>>> GetPendingReview(
    CancellationToken cancellationToken)
    {
        // Fetch the raw pending events
        var pendingEvents = await events.GetByStatus(EventStatus.Pending, cancellationToken);

        var reviewList = new List<PendingEventReviewDto>();

        foreach (var pending in pendingEvents)
        {
            // Define a window around the event date to check for updates/drift (e.g., +/- 1 day)
            var eventDate = pending.StartUtc.Date;
            var startDateWindow = eventDate.AddDays(-1);
            var endDateWindow = eventDate.AddDays(1);

            // Fetch matching published events within that timeframe
            var matches = await events.GetPotentialMatches(
                startDateWindow,
                endDateWindow,
                pending.Title,
                cancellationToken);

            reviewList.Add(new PendingEventReviewDto
            {
                PendingEvent = mapper.Map<EventDto>(pending),
                PotentialMatches = mapper.Map<IEnumerable<EventDto>>(matches)
            });
        }

        return Ok(reviewList);
    }

    [HttpPatch("{eventId:guid}")]
    [Authorize]
    public async Task<ActionResult> UpdateEvent(
        [FromRoute] Guid eventId,
        [FromBody] PatchEventRequest request,
        CancellationToken ct)
    {
        var @event = await events.GetById(eventId, ct);

        if (@event is not null)
        {
            if (request.CategoryId is not null && request.CategoryId != Guid.Empty)
            {
                @event.CategoryId = request.CategoryId;
            }

            if (request.Status is EventStatus status)
            {
                @event.Status = status;
                @event.StatusAtUtc = DateTimeOffset.UtcNow;
            }

            if (request.RequiresReview is not null)
            {
                @event.RequiresReview = request.RequiresReview.Value;
                @event.RequiresReviewAtUtc = DateTimeOffset.UtcNow;
            }

            await events.UpdateAsync(@event, ct);
        }

        return NoContent();
    }
}
