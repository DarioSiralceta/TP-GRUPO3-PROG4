using Availability.Application.Events.Queries.GetEvents;
using Availability.Application.Events.Queries.GetEventSeats;
using Microsoft.AspNetCore.Mvc;

namespace Availability.API.Controllers;

[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
    private readonly GetEventsUseCase _getEventsUseCase;

    private readonly GetEventSeatsUseCase _getEventSeatsUseCase;

    public EventsController(GetEventsUseCase getEventsUseCase,GetEventSeatsUseCase getEventSeatsUseCase )
    {
        _getEventsUseCase = getEventsUseCase;
        _getEventSeatsUseCase = getEventSeatsUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var events = await _getEventsUseCase.ExecuteAsync();

        return Ok(events);
    }

    [HttpGet("{eventId:int}/seats")]
    public async Task<IActionResult> GetSeats(int eventId)
    {
        try
        {
            var query = new GetEventSeatsQuery
            {
                EventId = eventId
            };

            var seats = await _getEventSeatsUseCase.ExecuteAsync(query);

            return Ok(seats);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}