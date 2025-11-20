using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Tendril.Api.Dtos;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly IEventRepository _events;
    private readonly IMapper _mapper;

    public EventsController(IEventRepository events, IMapper mapper)
    {
        _events = events;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _events.GetAllAsync(cancellationToken);
        return Ok(_mapper.Map<IEnumerable<EventDto>>(list));
    }
}
