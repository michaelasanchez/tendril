using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Api.Controllers;

[ApiController]
[Route("scrapers/{scraperId:guid}/classification-rules")]
public class ClassificationRulesController(IClassificationRuleRepository rules) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClassificationRuleDto>>> Get(Guid scraperId, CancellationToken cancellationToken)
    {
        var list = await rules.GetByScraperIdAsync(scraperId, cancellationToken);

        var dtos = list.Select(r => new ClassificationRuleDto(
            r.Id,
            r.ScraperDefinitionId,
            r.Order,
            r.Disabled,
            r.SourceJsonPath,
            r.ConditionType,
            r.ConditionValue,
            [.. r.Assignments.Select(a => new RuleAssignmentDto(a.Id, a.CategoryId, a.TagId))]
        ));

        return Ok(dtos);
    }

    [HttpPost]
    public async Task<ActionResult<ClassificationRuleDto>> Create(Guid scraperId, [FromBody] CreateClassificationRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = new ScraperClassificationRule
        {
            Id = Guid.NewGuid(),
            ScraperDefinitionId = scraperId,
            Order = request.Order,
            Disabled = request.Disabled,
            SourceJsonPath = request.SourceJsonPath,
            ConditionType = request.ConditionType,
            ConditionValue = request.ConditionValue,
            Assignments = [.. request.Assignments.Select(a => new RuleAssignment { Id = default, CategoryId = a.CategoryId, TagId = a.TagId })]
        };

        await rules.AddAsync(rule, cancellationToken);

        var dto = new ClassificationRuleDto(
            rule.Id,
            rule.ScraperDefinitionId,
            rule.Order,
            rule.Disabled,
            rule.SourceJsonPath,
            rule.ConditionType,
            rule.ConditionValue,
            [.. rule.Assignments.Select(a => new RuleAssignmentDto(a.Id, a.CategoryId, a.TagId))]
        );

        return CreatedAtAction(nameof(Get), new { scraperId }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid scraperId, Guid id, [FromBody] UpdateClassificationRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await rules.GetByIdAsync(id, cancellationToken);
        if (rule is null) return NotFound();


        if (request.ScraperDefinitionId is not null) rule.ScraperDefinitionId = request.ScraperDefinitionId.Value;
        if (request.Order is not null) rule.Order = request.Order.Value;
        if (request.Disabled is not null) rule.Disabled = request.Disabled.Value;
        if (request.SourceJsonPath is not null) rule.SourceJsonPath = request.SourceJsonPath;
        if (request.ConditionType is not null) rule.ConditionType = request.ConditionType.Value;
        if (request.ConditionValue is not null) rule.ConditionValue = request.ConditionValue;
        if (request.Assignments is not null && request.Assignments.Count > 0)
        {
            rule.Assignments = [.. request.Assignments.Select(x => new RuleAssignment {
                Id = x.Id ?? default,
                ScraperClassificationRuleId = scraperId,
                CategoryId = x.CategoryId,
                TagId = x.TagId
            })];
        }

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
