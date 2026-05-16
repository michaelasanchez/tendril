namespace Tendril.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
using Tendril.Core.Domain;
using Tendril.Core.Interfaces.Repositories;

[ApiController]
[Route("scrapers/{scraperId:guid}/attempt-histories")]
[Authorize]
public sealed class AttemptHistoryController(IAttemptHistoryRepository query) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AttemptHistoryDto>>>
        GetAttemptHistories(
            Guid scraperId,
            CancellationToken ct)
    {
        var attempts = await query.GetAttemptHistories(scraperId, ct);

        var resources = attempts.Select(a => new AttemptHistoryDto
        {
            Id = a.Id,
            StartTimeUtc = a.StartTimeUtc,
            EndTimeUtc = a.EndTimeUtc,
            Success = a.Success,
            Extracted = a.Extracted,
            Mapped = a.Mapped,
            Created = a.Created,
            Updated = a.Updated,
            Skipped = a.Skipped,
            Errored = a.Errored,
            ErrorMessage = a.ErrorMessage
        });

        return Ok(resources);
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PagedResponse<AttemptHistoryDto>>> GetAttemptHistories(
        Guid scraperId,
        [FromQuery] int? limit,
        [FromQuery] Guid? cursor,
        CancellationToken ct)
    {
        var pagedAttempts = await query.GetPagedAttemptsAsync(scraperId, limit, cursor, ct);

        return Ok(new PagedResponse<AttemptHistoryDto>
        {
            TotalCount = pagedAttempts.TotalCount,
            HasNextPage = pagedAttempts.HasNextPage,
            NextCursor = pagedAttempts.NextCursor,
            Items = pagedAttempts.Items.Select(a => new AttemptHistoryDto
            {
                Id = a.Id,
                StartTimeUtc = a.StartTimeUtc,
                EndTimeUtc = a.EndTimeUtc,
                Success = a.Success,
                Extracted = a.Extracted,
                Mapped = a.Mapped,
                Created = a.Created,
                Updated = a.Updated,
                Skipped = a.Skipped,
                Errored = a.Errored,
                ErrorMessage = a.ErrorMessage
            }).ToList()
        });
    }

    [HttpGet("{attemptId:guid}")]
    public async Task<ActionResult<AttemptHistoryDto>> GetAttemptById(
        Guid attemptId,
        CancellationToken ct)
    {
        var a = await query.GetAttemptByIdAsync(attemptId, ct);
        if (a == null) return NotFound();

        return Ok(new AttemptHistoryDto
        {
            Id = a.Id,
            StartTimeUtc = a.StartTimeUtc,
            EndTimeUtc = a.EndTimeUtc,
            Success = a.Success,
            Extracted = a.Extracted,
            Mapped = a.Mapped,
            Created = a.Created,
            Updated = a.Updated,
            Skipped = a.Skipped,
            Errored = a.Errored,
            ErrorMessage = a.ErrorMessage
        });
    }

    [HttpGet("{attemptId:guid}/raw-events")]
    public async Task<ActionResult<PagedResponse<ScrapedEventRawDto>>> GetRawEvents(
        Guid attemptId,
        [FromQuery] int? limit,
        [FromQuery] Guid? cursor,
        CancellationToken ct)
    {
        var pagedRaw = await query.GetRawEventsByAttemptAsync(attemptId, limit, cursor, ct);

        return Ok(new PagedResponse<ScrapedEventRawDto>
        {
            TotalCount = pagedRaw.TotalCount,
            HasNextPage = pagedRaw.HasNextPage,
            NextCursor = pagedRaw.NextCursor,
            Items = pagedRaw.Items.Select(r => new ScrapedEventRawDto
            {
                Id = r.Id,
                EventId = r.EventId,
                RawDataJson = r.RawDataJson,
                ScrapedAtUtc = r.ScrapedAtUtc
            }).ToList()
        });
    }

    [HttpGet("{attemptId:guid}/revisions")]
    public async Task<ActionResult<List<EventRevisionDto>>> GetRevisions(
        Guid attemptId,
        CancellationToken ct)
    {
        var revisions = await query.GetRevisionsByAttemptAsync(attemptId, ct);

        return Ok(revisions.Select(r => new EventRevisionDto
        {
            Id = r.Id,
            EventId = r.EventId,
            EventTitle = r.Event?.Title,
            RawEventId = r.RawEventId,
            ChangedAtUtc = r.ChangedAtUtc,
            Reason = r.Reason,
            ChangedFieldsJson = r.ChangedFieldsJson
        }).ToList());
    }
}