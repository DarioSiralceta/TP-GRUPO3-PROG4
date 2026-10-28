// src/Booking.Service/Services/BookingService.cs
using Booking.Infrastructure.Entities;
using Booking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Booking.Service.Services;

public interface IBookingService
{
    Task<IEnumerable<BookingEntity>> GetAllBookingsAsync();
    Task<BookingEntity?> GetBookingByIdAsync(Guid id);
    Task<BookingEntity> CreateRetentionAsync(string eventId, string seatId, string userId);
}

public class BookingService : IBookingService
{
    private readonly BookingDbContext _context;

    public BookingService(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BookingEntity>> GetAllBookingsAsync()
    {
        return await _context.Bookings.ToListAsync();
    }

    public async Task<BookingEntity?> GetBookingByIdAsync(Guid id)
    {
        return await _context.Bookings.FindAsync(id);
    }
    
    public async Task<BookingEntity> CreateRetentionAsync(string eventId, string seatId, string userId)
    {
        var booking = new BookingEntity
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            SeatId = seatId,
            UserId = userId,
            Status = "Retenida", //[span_4](start_span)[span_4](end_span)
            ExpiresAt = DateTime.UtcNow.AddMinutes(2) //[span_5](start_span)[span_5](end_span)
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        return booking;
    }
}
