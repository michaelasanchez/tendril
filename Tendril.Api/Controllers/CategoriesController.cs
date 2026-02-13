using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Api.Controllers;

[ApiController]
[Route("categories")]
public class CategoriesController(ICategoryRepository categories, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await categories.GetAllAsync(cancellationToken);

        return Ok(mapper.Map<IEnumerable<CategoryDto>>(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(id, cancellationToken);

        if (category is null) return NotFound();

        return Ok(mapper.Map<CategoryDto>(category));
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(CategoryDto request, CancellationToken cancellationToken)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description
        };

        await categories.AddAsync(category, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = category.Id }, mapper.Map<CategoryDto>(category));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CategoryDto request, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(id, cancellationToken);

        if (category is null) return NotFound();

        category.Name = request.Name;
        category.Description = request.Description;

        await categories.UpdateAsync(category, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(id, cancellationToken);

        if (category is null) return NotFound();

        await categories.DeleteAsync(category, cancellationToken);

        return NoContent();
    }
}
