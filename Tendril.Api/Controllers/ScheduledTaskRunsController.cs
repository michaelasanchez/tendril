using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Api.Controllers;

[ApiController]
[Route("scheduled-task-runs")]
[Authorize]
public sealed class ScheduledTaskRunsController(IScheduledTaskRunRepository runs) : ControllerBase
{
    [HttpGet("recent")]
    public async Task<ActionResult<IReadOnlyList<ScheduledTaskRunDto>>> GetRecent(
        [FromQuery] int limit = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await runs.GetRecentAsync(Math.Clamp(limit, 1, 100), cancellationToken);

        return Ok(result.Select(run =>
        {
            var completedAttempts = run.AttemptHistories
                .Where(attempt => attempt.ErrorMessage != "Running...")
                .ToList();

            return new ScheduledTaskRunDto(
                run.Id,
                run.ScheduledTaskId,
                run.ScheduledTask.Name,
                run.Status.ToString(),
                run.StartTimeUtc,
                run.EndTimeUtc,
                run.RetryCount,
                run.AttemptHistories.Count,
                completedAttempts.Count(attempt => attempt.Success),
                completedAttempts.Count(attempt => !attempt.Success),
                run.AttemptHistories.Count(attempt => attempt.ErrorMessage == "Running..."),
                run.AttemptHistories
                    .OrderBy(attempt => attempt.StartTimeUtc)
                    .Select(attempt => new ScheduledTaskRunAttemptDto(
                        attempt.Id,
                        attempt.ScraperDefinitionId,
                        attempt.ScraperDefinition.Name,
                        attempt.ErrorMessage == "Running..."
                            ? "Running"
                            : attempt.Success ? "Succeeded" : "Failed",
                        attempt.StartTimeUtc,
                        attempt.ErrorMessage == "Running..." ? null : attempt.EndTimeUtc,
                        attempt.Extracted,
                        attempt.Mapped,
                        attempt.Created,
                        attempt.Updated,
                        attempt.Skipped,
                        attempt.Errored,
                        attempt.ErrorMessage == "Running..." ? null : attempt.ErrorMessage))
                    .ToList());
        }).ToList());
    }
}