using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Domain.Enums;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Api.Controllers;

[ApiController]
[Route("scheduled-tasks")]
[Authorize]
public class ScheduledTasksController(
    IScheduledTaskRepository tasks,
    IScraperRepository scrapers,
    IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScheduledTaskDto>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await tasks.GetAllAsync(cancellationToken);
        return Ok(mapper.Map<IEnumerable<ScheduledTaskDto>>(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ScheduledTaskDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var task = await tasks.GetByIdWithScrapersAsync(id, cancellationToken);
        if (task is null) return NotFound();

        return Ok(mapper.Map<ScheduledTaskDto>(task));
    }

    [HttpPost]
    public async Task<ActionResult<ScheduledTaskDto>> Create(CreateScheduledTaskRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SelectionStrategy>(request.SelectionStrategy, true, out var strategy))
        {
            return BadRequest("Invalid SelectionStrategy value.");
        }

        var task = new ScheduledTask
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Notes = request.Notes ?? string.Empty,
            IsDisabled = request.IsDisabled,
            CronExpression = request.CronExpression,
            SelectionStrategy = strategy,
            Status = "Idle",
            // Worker or a backing service can evaluate the initial NextRunAtUtc based on the CronExpression,
            // default it to immediate execution check for now.
            NextRunAtUtc = DateTimeOffset.UtcNow
        };

        // If specific scrapers are assigned at creation time
        if (strategy == SelectionStrategy.Selected && request.ScraperIds is not null)
        {
            foreach (var scraperId in request.ScraperIds)
            {
                var scraper = await scrapers.GetByIdAsync(scraperId, cancellationToken);
                if (scraper != null)
                {
                    task.ScraperDefinitions.Add(scraper);
                }
            }
        }

        await tasks.AddAsync(task, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = task.Id },
            mapper.Map<ScheduledTaskDto>(task));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateScheduledTaskRequest request, CancellationToken cancellationToken)
    {
        // Fetch with ScraperDefinitions included to manipulate the junction graph
        var task = await tasks.GetByIdWithScrapersAsync(id, cancellationToken);
        if (task is null) return NotFound();

        // Update Scalar values
        if (request.Name is not null) task.Name = request.Name;
        if (request.Notes is not null) task.Notes = request.Notes;
        if (request.IsDisabled is not null) task.IsDisabled = request.IsDisabled.Value;
        if (request.CronExpression is not null) task.CronExpression = request.CronExpression;
        if (request.NextRunAtUtc is not null) task.NextRunAtUtc = request.NextRunAtUtc.Value;
        if (request.Status is not null) task.Status = request.Status;

        if (request.SelectionStrategy is not null &&
            Enum.TryParse<SelectionStrategy>(request.SelectionStrategy, true, out var strategy))
        {
            task.SelectionStrategy = strategy;
        }

        // Many-to-Many Relationship Synchronization
        if (request.ScraperIds is not null)
        {
            // Clear associations immediately if strategy shifts to "All"
            if (task.SelectionStrategy == SelectionStrategy.All)
            {
                task.ScraperDefinitions.Clear();
            }
            else
            {
                // REMOVE: Unmap scrapers no longer in the payload request
                var scrapersToRemove = task.ScraperDefinitions
                    .Where(s => !request.ScraperIds.Contains(s.Id))
                    .ToList();

                foreach (var toRemove in scrapersToRemove)
                {
                    task.ScraperDefinitions.Remove(toRemove);
                }

                // ADD: Map new scrapers not already tracked in this task
                var existingScraperIds = task.ScraperDefinitions.Select(s => s.Id).ToList();
                var scraperIdsToAdd = request.ScraperIds.Where(id => !existingScraperIds.Contains(id));

                foreach (var scraperId in scraperIdsToAdd)
                {
                    var scraper = await scrapers.GetByIdAsync(scraperId, cancellationToken);
                    if (scraper is not null)
                    {
                        task.ScraperDefinitions.Add(scraper);
                    }
                }
            }
        }

        await tasks.UpdateAsync(task, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var task = await tasks.GetByIdAsync(id, cancellationToken);
        if (task is null) return NotFound();

        await tasks.DeleteAsync(task, cancellationToken);
        return NoContent();
    }
}