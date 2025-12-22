namespace Tendril.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
using Tendril.Core.Interfaces.Repositories;

[ApiController]
[Route("api/scrapers/{scraperId:guid}/attempt-histories")]
public sealed class AttemptHistoryController : ControllerBase
{
    private readonly IAttemptHistoryRepository _attempts;

    public AttemptHistoryController(IAttemptHistoryRepository query)
    {
        _attempts = query;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AttemptHistoryDto>>>
        GetAttemptHistories(
            Guid scraperId,
            CancellationToken ct)
    {
        var attempts = await _attempts.GetAttemptHistories(scraperId, ct);

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
            ErrorMessage = a.ErrorMessage
        });

        return Ok(resources);
    }
}
