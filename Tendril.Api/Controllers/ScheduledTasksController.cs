using AutoMapper;
using Cronos;
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
            Status = ScheduledTaskStatus.Idle,
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

        DateTimeOffset? nextRunAtUtc = null;
        if (request.CronExpression is not null &&
            !string.Equals(request.CronExpression, task.CronExpression, StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(request.CronExpression))
            {
                return BadRequest("Cron expression cannot be empty.");
            }

            try
            {
                var cron = CronExpression.Parse(request.CronExpression);
                var next = cron.GetNextOccurrence(DateTime.UtcNow);
                if (next is null)
                {
                    return BadRequest("Cron expression has no future occurrence.");
                }

                nextRunAtUtc = new DateTimeOffset(next.Value, TimeSpan.Zero);
            }
            catch (CronFormatException)
            {
                return BadRequest("Cron expression is invalid.");
            }
        }

        // Update Scalar values
        if (request.Name is not null) task.Name = request.Name;
        if (request.Notes is not null) task.Notes = request.Notes;
        if (request.IsDisabled is not null) task.IsDisabled = request.IsDisabled.Value;
        if (request.CronExpression is not null)
        {
            task.CronExpression = request.CronExpression;
            if (nextRunAtUtc.HasValue) task.NextRunAtUtc = nextRunAtUtc.Value;
        }
        if (request.NextRunAtUtc is not null) task.NextRunAtUtc = request.NextRunAtUtc.Value;
        if (request.Status is not null) task.Status = request.Status.Value;

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

    [HttpPatch("{id:guid}/scrapers")]
    public async Task<IActionResult> UpdateScrapers(
        Guid id,
        UpdateScheduledTaskScrapersRequest request,
        CancellationToken cancellationToken)
    {
        var task = await tasks.GetByIdWithScrapersAsync(id, cancellationToken);
        if (task is null) return NotFound();
        if (task.SelectionStrategy != SelectionStrategy.Selected)
        {
            return Conflict("Scraper assignments can only be changed for schedules using the Selected strategy.");
        }

        var scraperIdsToAdd = (request.Add ?? []).Distinct().ToList();
        var scraperIdsToRemove = (request.Remove ?? []).Distinct().ToList();
        if (scraperIdsToAdd.Intersect(scraperIdsToRemove).Any())
        {
            return BadRequest("A scraper cannot be added and removed in the same request.");
        }

        var scraperDefinitions = new Dictionary<Guid, ScraperDefinition>();
        foreach (var scraperId in scraperIdsToAdd)
        {
            var scraper = await scrapers.GetByIdAsync(scraperId, cancellationToken);
            if (scraper is null) return NotFound($"Scraper '{scraperId}' was not found.");
            scraperDefinitions.Add(scraperId, scraper);
        }

        foreach (var scraperId in scraperIdsToRemove)
        {
            if (await scrapers.GetByIdAsync(scraperId, cancellationToken) is null)
            {
                return NotFound($"Scraper '{scraperId}' was not found.");
            }
        }

        var existingScraperIds = task.ScraperDefinitions.Select(scraper => scraper.Id).ToHashSet();
        foreach (var scraperId in scraperIdsToAdd)
        {
            if (existingScraperIds.Add(scraperId))
            {
                task.ScraperDefinitions.Add(scraperDefinitions[scraperId]);
            }
        }

        foreach (var scraper in task.ScraperDefinitions
                     .Where(scraper => scraperIdsToRemove.Contains(scraper.Id))
                     .ToList())
        {
            task.ScraperDefinitions.Remove(scraper);
        }

        await tasks.UpdateAsync(task, cancellationToken);
        return NoContent();
    }

    [HttpPost("{destinationTaskId:guid}/scrapers/transfer")]
    public async Task<IActionResult> TransferScrapers(
        Guid destinationTaskId,
        TransferScheduledTaskScrapersRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SourceTaskId == destinationTaskId)
        {
            return BadRequest("Source and destination schedules must be different.");
        }

        if (!string.Equals(request.Mode, "copy", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.Mode, "move", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Mode must be 'copy' or 'move'.");
        }

        var scraperIds = request.ScraperIds?.Distinct().ToList();
        if (scraperIds is null || scraperIds.Count == 0)
        {
            return BadRequest("At least one scraper ID is required.");
        }

        var sourceTask = await tasks.GetByIdWithScrapersAsync(request.SourceTaskId, cancellationToken);
        if (sourceTask is null) return NotFound($"Source schedule '{request.SourceTaskId}' was not found.");

        var destinationTask = await tasks.GetByIdWithScrapersAsync(destinationTaskId, cancellationToken);
        if (destinationTask is null) return NotFound($"Destination schedule '{destinationTaskId}' was not found.");

        if (sourceTask.SelectionStrategy != SelectionStrategy.Selected ||
            destinationTask.SelectionStrategy != SelectionStrategy.Selected)
        {
            return Conflict("Both schedules must use the Selected strategy to transfer scraper assignments.");
        }

        var sourceScrapers = sourceTask.ScraperDefinitions
            .Where(scraper => scraperIds.Contains(scraper.Id))
            .ToDictionary(scraper => scraper.Id);
        if (sourceScrapers.Count != scraperIds.Count)
        {
            return BadRequest("One or more scraper IDs are not assigned to the source schedule.");
        }

        var destinationScraperIds = destinationTask.ScraperDefinitions
            .Select(scraper => scraper.Id)
            .ToHashSet();
        foreach (var scraperId in scraperIds)
        {
            if (destinationScraperIds.Add(scraperId))
            {
                destinationTask.ScraperDefinitions.Add(sourceScrapers[scraperId]);
            }
        }

        if (string.Equals(request.Mode, "move", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var scraperId in scraperIds)
            {
                sourceTask.ScraperDefinitions.Remove(sourceScrapers[scraperId]);
            }
        }

        await tasks.UpdateAsync(destinationTask, cancellationToken);
        return NoContent();
    }
}