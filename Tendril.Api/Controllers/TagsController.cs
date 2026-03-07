using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Api.Controllers;

[ApiController]
[Route("tags")]
public class TagsController(ITagRepository tags, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TagDto>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await tags.GetAllAsync(cancellationToken);

        return Ok(mapper.Map<IEnumerable<TagDto>>(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TagDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var tag = await tags.GetByIdAsync(id, cancellationToken);

        if (tag is null) return NotFound();

        return Ok(mapper.Map<TagDto>(tag));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<TagDto>> Create(TagDto request, CancellationToken cancellationToken)
    {
        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            Name = request.Name
        };

        await tags.AddAsync(tag, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = tag.Id }, mapper.Map<TagDto>(tag));
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, TagDto request, CancellationToken cancellationToken)
    {
        var tag = await tags.GetByIdAsync(id, cancellationToken);

        if (tag is null) return NotFound();

        tag.Name = request.Name;

        await tags.UpdateAsync(tag, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var tag = await tags.GetByIdAsync(id, cancellationToken);

        if (tag is null) return NotFound();

        await tags.DeleteAsync(tag, cancellationToken);

        return NoContent();
    }
}
