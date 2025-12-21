using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
using Tendril.Core.Domain.Entities;

namespace Tendril.Api.Controllers;

[ApiController]
[Route("api/scrapers/{scraperId:guid}/selectors")]
public class SelectorsController : ControllerBase
{
    private readonly ISelectorRepository _selectors;

    public SelectorsController(ISelectorRepository selectors)
    {
        _selectors = selectors;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScraperSelectorDto>>> GetSelectors(Guid scraperId, CancellationToken cancellationToken)
    {
        var list = await _selectors.GetByScraperIdAsync(scraperId, cancellationToken);

        return Ok(list.Select(s => new ScraperSelectorDto(
            s.Id,
            s.FieldName,
            s.Selector,
            s.Order,
            s.Root,
            s.Type.ToString(),
            s.AttributeName,
            s.Delay
        )));
    }

    [HttpPost]
    public async Task<ActionResult> CreateSelector(Guid scraperId, [FromBody] CreateSelectorRequest request, CancellationToken cancellationToken)
    {
        var selector = new ScraperSelector
        {
            Id = Guid.NewGuid(),
            ScraperDefinitionId = scraperId,
            FieldName = request.FieldName,
            Selector = request.Selector,
            Order = request.Order,
            Root = request.Root,
            Type = request.Type,
            AttributeName = request.Attribute,
            Delay = request.Delay,
        };

        await _selectors.AddAsync(selector, cancellationToken);

        return CreatedAtAction(nameof(GetSelectors), new { scraperId }, selector);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> UpdateSelector(Guid scraperId, Guid id, UpdateSelectorRequest request, CancellationToken cancellationToken)
    {
        var selector = await _selectors.GetByIdAsync(id, cancellationToken);

        if (selector is null) return NotFound();

        selector.FieldName = request.FieldName ?? selector.FieldName;
        selector.Selector = request.Selector ?? selector.Selector;
        selector.Order = request.Order ?? selector.Order;
        selector.Root = request.Root ?? selector.Root;
        selector.Type = request.Type ?? selector.Type;
        selector.AttributeName = request.Attribute ?? selector.AttributeName;
        selector.Delay = request.Delay ?? selector.Delay;

        await _selectors.UpdateAsync(selector, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteSelector(Guid scraperId, Guid id, CancellationToken cancellationToken)
    {
        var selector = await _selectors.GetByIdAsync(id, cancellationToken);
        if (selector is null) return NotFound();

        await _selectors.DeleteAsync(selector, cancellationToken);

        return NoContent();
    }
}
