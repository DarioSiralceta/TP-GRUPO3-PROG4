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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookingEntity>>> GetBookings()
    {
        var bookings = await _bookingService.GetAllBookingsAsync();
        return Ok(bookings);
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

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<BookingEntity>> CreateBooking(string eventId, string seatId, string userId)
    {
        var booking = await _bookingService.CreateRetentionAsync(eventId, seatId, userId);
        return Ok(booking);
    }
}
