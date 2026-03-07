namespace Tendril.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
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
}
