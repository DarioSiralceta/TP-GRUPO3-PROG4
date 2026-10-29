using Booking.Infrastructure.Entities;
using Booking.Service.DTOs;
using Booking.Service.Services;
using Microsoft.AspNetCore.Mvc;

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

    [HttpGet("events/{eventId}/seats/{seatId}")]
    public async Task<ActionResult<SeatStatusDto>> GetSeatStatus(string eventId, string seatId) // Franco Brienza
    {
        var seatStatus = await _bookingService.GetSeatStatusAsync(eventId, seatId);
        return Ok(seatStatus);
    }

    [HttpGet("events/{eventId}/occupied-seats")]
    public async Task<ActionResult<List<string>>> GetOccupiedSeats(string eventId) // Franco Brienza
    {
        var occupiedSeats = await _bookingService.GetOccupiedSeatsAsync(eventId);
        return Ok(occupiedSeats);
    }

    [HttpPost]
    public async Task<ActionResult<BookingEntity>> CreateBooking(string eventId, string seatId, string userId)
    {
        var booking = await _bookingService.CreateRetentionAsync(eventId, seatId, userId);
        return Ok(booking);
    }
}
