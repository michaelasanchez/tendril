using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
using Tendril.Core.Domain.Entities;

namespace Tendril.Api.Controllers;

[ApiController]
[Route("scrapers/{scraperId:guid}/actions")]
[Authorize]
public class ActionsController : ControllerBase
{
    private readonly IActionRepository _actions;

    public ActionsController(IActionRepository actions)
    {
        _actions = actions;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScraperActionDto>>> GetActions(Guid scraperId, CancellationToken cancellationToken)
    {
        var list = await _actions.GetByScraperIdAsync(scraperId, cancellationToken);

        return Ok(list.Select(s => new ScraperActionDto(
            s.Id,
            s.Name,
            s.FieldName,
            s.Selector,
            s.Order,
            s.Root,
            s.Type.ToString(),
            s.AttributeName,
            s.Delay,
            s.ConstantValue,
            s.InteractionValue,
            s.ChildScraperDefinitionId,
            s.AllowDuplicateUrls,
            s.IsPaginationTrigger,
            s.Disabled
        )));
    }

    [HttpPost]
    public async Task<ActionResult> CreateAction(Guid scraperId, [FromBody] CreateActionRequest request, CancellationToken cancellationToken)
    {
        var action = new ScraperAction
        {
            Id = Guid.NewGuid(),
            ScraperDefinitionId = scraperId,
            Name = request.Name,
            FieldName = request.FieldName,
            Selector = request.Selector,
            Order = request.Order,
            Root = request.Root,
            Type = request.Type,
            AttributeName = request.Attribute,
            Delay = request.Delay,
            ConstantValue = request.ConstantValue,
            InteractionValue = request.InteractionValue,
            ChildScraperDefinitionId = request.ChildScraperId,
            AllowDuplicateUrls = request.IgnoreDuplicateUrls,
            IsPaginationTrigger = request.IsPaginationTrigger,
            Disabled = request.Disabled
        };

        await _actions.AddAsync(action, cancellationToken);

        return CreatedAtAction(nameof(GetActions), new { scraperId }, action);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> UpdateAction(Guid scraperId, Guid id, UpdateActionRequest request, CancellationToken cancellationToken)
    {
        var action = await _actions.GetByIdAsync(id, cancellationToken);

        if (action is null) return NotFound();

        action.Name = request.Name ?? action.Name;
        action.FieldName = request.FieldName ?? action.FieldName;
        action.Selector = request.Selector ?? action.Selector;
        action.Order = request.Order ?? action.Order;
        action.Root = request.Root ?? action.Root;
        action.Type = request.Type ?? action.Type;
        action.AttributeName = request.Attribute;
        action.Delay = request.Delay;
        action.ConstantValue = request.ConstantValue;
        action.InteractionValue = request.InteractionValue;
        action.ChildScraperDefinitionId = request.ChildScraperId;
        action.AllowDuplicateUrls = request.IgnoreDuplicateUrls ?? action.AllowDuplicateUrls;
        action.IsPaginationTrigger = request.IsPaginationTrigger ?? action.IsPaginationTrigger;
        action.Disabled = request.Disabled ?? action.Disabled;

        await _actions.UpdateAsync(action, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteAction(Guid scraperId, Guid id, CancellationToken cancellationToken)
    {
        var action = await _actions.GetByIdAsync(id, cancellationToken);
        if (action is null) return NotFound();

        await _actions.DeleteAsync(action, cancellationToken);

        return NoContent();
    }
}
