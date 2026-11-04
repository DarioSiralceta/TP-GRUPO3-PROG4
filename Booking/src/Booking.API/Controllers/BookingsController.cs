using Booking.Infrastructure.Entities;
using Booking.Service.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Booking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [Authorize(Policy = "CanReadBooking")]
    [HttpGet("{id}")]
    public async Task<ActionResult<BookingEntity>> GetBooking(Guid id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);
        if (booking == null)
        {
            return NotFound();
        }
        return Ok(booking);
    }

    [HttpGet("events/{eventId}/occupied-seats")]
    public async Task<ActionResult<List<string>>> GetOccupiedSeats(string eventId) // Franco Brienza
    {
        var occupiedSeats = await _bookingService.GetOccupiedSeatsAsync(eventId);
        return Ok(occupiedSeats);
    }
}
