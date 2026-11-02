using Availability.Application.Events.Queries.GetEvents;
using Microsoft.AspNetCore.Mvc;

namespace Availability.API.Controllers;

[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
    private readonly GetEventsUseCase _getEventsUseCase;

    public EventsController(GetEventsUseCase getEventsUseCase)
    {
        _getEventsUseCase = getEventsUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var events = await _getEventsUseCase.ExecuteAsync();

        return Ok(events);
    }
}