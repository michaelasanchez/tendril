using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Api.Controllers;

[ApiController]
[Route("api/scrapers/{scraperId:guid}/mapping-rules")]
public class MappingRulesController : ControllerBase
{
    private readonly IMappingRuleRepository _rules;

    public MappingRulesController(IMappingRuleRepository rules)
    {
        _rules = rules;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MappingRuleDto>>> Get(Guid scraperId, CancellationToken cancellationToken)
    {
        var list = await _rules.GetByScraperIdAsync(scraperId, cancellationToken);
        var dtos = list.Select(r => new MappingRuleDto(
            r.Id,
            r.TargetField,
            r.SourceField,
            r.CombineWithField,
            r.TransformType
        ));

        return Ok(dtos);
    }

    [HttpPost]
    public async Task<ActionResult<MappingRuleDto>> Create(Guid scraperId, [FromBody] CreateMappingRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = new ScraperMappingRule
        {
            Id = Guid.NewGuid(),
            ScraperDefinitionId = scraperId,
            TargetField = request.TargetField,
            SourceField = request.SourceField,
            CombineWithField = request.CombineWithField,
            TransformType = request.TransformType
        };

        await _rules.AddAsync(rule, cancellationToken);

        var dto = new MappingRuleDto(
            rule.Id,
            rule.TargetField,
            rule.SourceField,
            rule.CombineWithField,
            rule.TransformType
        );

        return CreatedAtAction(nameof(Get), new { scraperId }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid scraperId, Guid id, [FromBody] UpdateMappingRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await _rules.GetByIdAsync(id, cancellationToken);
        if (rule is null) return NotFound();

        if (request.TargetField is not null) rule.TargetField = request.TargetField;
        if (request.SourceField is not null) rule.SourceField = request.SourceField;
        if (request.CombineWithField is not null) rule.CombineWithField = request.CombineWithField;
        if (request.TransformType is not null) rule.TransformType = request.TransformType ?? Core.Domain.Enums.TransformType.None;

        await _rules.UpdateAsync(rule, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid scraperId, Guid id, CancellationToken cancellationToken)
    {
        var rule = await _rules.GetByIdAsync(id, cancellationToken);
        if (rule is null) return NotFound();

        await _rules.DeleteAsync(rule, cancellationToken);
        return NoContent();
    }
}
