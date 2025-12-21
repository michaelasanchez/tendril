using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Api.Controllers;

[ApiController]
[Route("api/scrapers/{scraperId:guid}/mapping-rules")]
public class MappingRulesController(IMappingRuleRepository rules) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MappingRuleDto>>> Get(Guid scraperId, CancellationToken cancellationToken)
    {
        var list = await rules.GetByScraperIdAsync(scraperId, cancellationToken);

        var dtos = list.Select(r => new MappingRuleDto(
            r.Id,
            r.TargetField,
            r.SourceField,
            r.CombineWithField,
            r.Order,
            r.TransformType,
            r.Format,
            r.RegexPattern,
            r.RegexReplacement,
            r.SplitDelimiter
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
            Order = request.Order,
            TransformType = request.TransformType,
            Format = request.Format,
            RegexPattern = request.RegexPattern,
            RegexReplacement = request.RegexReplacement,
            SplitDelimiter = request.SplitDelimiter
        };

        await rules.AddAsync(rule, cancellationToken);

        var dto = new MappingRuleDto(
            rule.Id,
            rule.TargetField,
            rule.SourceField,
            rule.CombineWithField,
            rule.Order,
            rule.TransformType,
            rule.Format,
            rule.RegexPattern,
            rule.RegexReplacement,
            rule.SplitDelimiter
        );

        return CreatedAtAction(nameof(Get), new { scraperId }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid scraperId, Guid id, [FromBody] UpdateMappingRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await rules.GetByIdAsync(id, cancellationToken);
        if (rule is null) return NotFound();

        if (request.TargetField is not null) rule.TargetField = request.TargetField;
        if (request.SourceField is not null) rule.SourceField = request.SourceField;
        if (request.CombineWithField is not null) rule.CombineWithField = request.CombineWithField;
        if (request.Order is not null) rule.Order = request.Order.Value;
        if (request.TransformType is not null) rule.TransformType = request.TransformType ?? Core.Domain.Enums.TransformType.None;
        if (request.Format is not null) rule.Format = request.Format;
        if (request.RegexPattern is not null) rule.RegexPattern = request.RegexPattern;
        if (request.RegexReplacement is not null) rule.RegexReplacement = request.RegexReplacement;
        if (request.SplitDelimiter is not null) rule.SplitDelimiter = request.SplitDelimiter;

        await rules.UpdateAsync(rule, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid scraperId, Guid id, CancellationToken cancellationToken)
    {
        var rule = await rules.GetByIdAsync(id, cancellationToken);
        if (rule is null) return NotFound();

        await rules.DeleteAsync(rule, cancellationToken);
        return NoContent();
    }
}
