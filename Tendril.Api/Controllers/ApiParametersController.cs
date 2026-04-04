using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Api.Controllers;

[ApiController]
[Route("scrapers/{scraperId:guid}/api-parameters")]
[Authorize]
public class ApiParametersController(IApiParameterRepository parameters) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApiParameterDto>>> Get(Guid scraperId, CancellationToken cancellationToken)
    {
        var list = await parameters.GetByScraperIdAsync(scraperId, cancellationToken);

        var dtos = list.Select(r => new ApiParameterDto(
            r.Id,
            r.ScraperDefinitionId,
            r.Key,
            r.Template,
            r.Source,
            r.Target,
            r.IsRequired
        ));

        return Ok(dtos);
    }

    [HttpPost]
    public async Task<ActionResult<ApiParameterDto>> Create(Guid scraperId, [FromBody] CreateApiParameterRequest request, CancellationToken cancellationToken)
    {
        var parameter = new ApiParameter
        {
            Id = Guid.NewGuid(),
            ScraperDefinitionId = scraperId,
            Key = request.Key,
            Template = request.Template,
            Source = request.Source,
            Target = request.Target,
            IsRequired = request.IsRequired
        };

        await parameters.AddAsync(parameter, cancellationToken);

        var dto = new ApiParameterDto(
            parameter.Id,
            parameter.ScraperDefinitionId,
            parameter.Key,
            parameter.Template,
            parameter.Source,
            parameter.Target,
            parameter.IsRequired
        );

        return CreatedAtAction(nameof(Get), new { scraperId }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid scraperId, Guid id, [FromBody] UpdateApiParameterRequest request, CancellationToken cancellationToken)
    {
        var parameter = await parameters.GetByIdAsync(id, cancellationToken);
        if (parameter is null) return NotFound();

        if (request.Key is not null) parameter.Key = request.Key;
        if (request.Template is not null) parameter.Template = request.Template;
        if (request.Source is not null) parameter.Source = request.Source.Value;
        if (request.Target is not null) parameter.Target = request.Target.Value;
        if (request.IsRequired is not null) parameter.IsRequired = request.IsRequired.Value;

        await parameters.UpdateAsync(parameter, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid scraperId, Guid id, CancellationToken cancellationToken)
    {
        var parameter = await parameters.GetByIdAsync(id, cancellationToken);
        if (parameter is null) return NotFound();

        await parameters.DeleteAsync(parameter, cancellationToken);
        return NoContent();
    }
}
