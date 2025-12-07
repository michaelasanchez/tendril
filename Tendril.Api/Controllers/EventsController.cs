using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController(IEventRepository events, IMapper mapper) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetAll(
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate,
        CancellationToken cancellationToken)
    {
        var list = await events.GetAllAsync(startDate ?? DateTime.Today.AddMonths(-1), endDate, cancellationToken);

        return Ok(mapper.Map<IEnumerable<EventDto>>(list));
    }
}
