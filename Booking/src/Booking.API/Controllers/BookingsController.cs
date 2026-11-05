using Booking.Infrastructure.Entities;
using Booking.Service.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Booking.Service.Interfaces;

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

    // AI contribution: Gemini Flash 3.8 — authorization for booking-by-ID.
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
    public async Task<ActionResult<List<string>>> GetOccupiedSeats(string eventId)
    {
        var occupiedSeats = await _bookingService.GetOccupiedSeatsAsync(eventId);
        return Ok(occupiedSeats);
    }
}
