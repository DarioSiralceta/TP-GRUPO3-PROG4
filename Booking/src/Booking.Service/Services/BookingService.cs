// src/Booking.Service/Services/BookingService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Booking.Infrastructure.Entities;
using Booking.Infrastructure.Persistence;
using Booking.Service.DTOs;
using Booking.Service.Services.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Booking.Service.Services;

public interface IBookingService
{
    Task<IEnumerable<BookingEntity>> GetAllBookingsAsync();
    Task<BookingEntity?> GetBookingByIdAsync(Guid id);
    Task<BookingEntity> CreateRetentionAsync(string eventId, string seatId, string userId);
    Task<BookingEntity> ProcessReservationRequestAsync(ReservationRequestedMessage request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Business service implementation for Booking following N-Layer architecture and Anemic Domain.
/// Handles seat validation, retention lifecycle, expiration, and payment orchestration.
/// </summary>
public class BookingService : IBookingService
{
    private readonly BookingDbContext _context;
    private readonly IPaymentServiceClient _paymentServiceClient;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        BookingDbContext context,
        IPaymentServiceClient paymentServiceClient,
        ILogger<BookingService> logger)
    {
        _context = context;
        _paymentServiceClient = paymentServiceClient;
        _logger = logger;
    }

    public async Task<IEnumerable<BookingEntity>> GetAllBookingsAsync()
    {
        var bookings = await _context.Bookings.ToListAsync();
        
        // Evaluate expired retentions
        var now = DateTime.UtcNow;
        bool hasChanges = false;
        foreach (var b in bookings)
        {
            if (b.Status == "Retenida" && b.ExpiresAt <= now)
            {
                b.Status = "Vencida";
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await _context.SaveChangesAsync();
        }

        return bookings;
    }

    public async Task<BookingEntity?> GetBookingByIdAsync(Guid id)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null)
        {
            return null;
        }

        // Domain rule 8: Expired retention transitions to Vencida
        if (booking.Status == "Retenida" && booking.ExpiresAt <= DateTime.UtcNow)
        {
            booking.Status = "Vencida";
            await _context.SaveChangesAsync();
        }

        return booking;
    }
    
    public async Task<BookingEntity> CreateRetentionAsync(string eventId, string seatId, string userId)
    {
        var now = DateTime.UtcNow;

        // Eagerly release any expired retentions for this seat
        var expiredBookings = await _context.Bookings
            .Where(b => b.EventId == eventId
                     && b.SeatId == seatId
                     && b.Status == "Retenida"
                     && b.ExpiresAt <= now)
            .ToListAsync();

        if (expiredBookings.Count > 0)
        {
            foreach (var b in expiredBookings)
            {
                b.Status = "Vencida";
            }
            await _context.SaveChangesAsync();
        }

        var booking = new BookingEntity
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            SeatId = seatId,
            UserId = userId,
            Status = "Retenida",
            ExpiresAt = now.AddMinutes(2),
            CreatedAt = now
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        return booking;
    }

    /// <summary>
    /// Processes reservation request messages received from Availability via Azure Service Bus.
    /// Flow steps:
    /// 4. Processing: Checks if the seat is free. If free, creates 'Retenida' booking. If occupied, creates 'Rechazada'.
    ///    Guaranteed at DB level via PostgreSQL Partial Unique Index: IX_bookings_EventId_SeatId_Active.
    /// 5. Payment: Calls Payment via gRPC to charge the retained booking.
    /// 6. Result: Confirms ('Confirmada') or rejects ('Rechazada', reason: 'Pago fallido') based on Payment result.
    /// </summary>
    public async Task<BookingEntity> ProcessReservationRequestAsync(
        ReservationRequestedMessage request, 
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // 1. Idempotency: Verify whether RequestId has already been processed
        var existingBooking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == request.RequestId, cancellationToken);
        if (existingBooking != null)
        {
            _logger.LogInformation("Request {RequestId} already processed with status {Status}. Returning existing record.",
                request.RequestId, existingBooking.Status);
            return existingBooking;
        }

        // 2. Eagerly release expired retentions for this seat so the partial unique index does not block re-booking
        var expiredBookings = await _context.Bookings
            .Where(b => b.EventId == request.EventId
                     && b.SeatId == request.SeatId
                     && b.Status == "Retenida"
                     && b.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        if (expiredBookings.Count > 0)
        {
            _logger.LogInformation("Releasing {Count} expired retention(s) for Seat {SeatId} in Event {EventId}.",
                expiredBookings.Count, request.SeatId, request.EventId);

            foreach (var b in expiredBookings)
            {
                b.Status = "Vencida";
            }
            await _context.SaveChangesAsync(cancellationToken);
        }

        // 3. Application-level check: Verify seat availability
        // Rule: "A seat is occupied while it has a Retenida (unexpired) or Confirmada booking."
        var isOccupied = await _context.Bookings.AnyAsync(b =>
            b.EventId == request.EventId &&
            b.SeatId == request.SeatId &&
            (b.Status == "Confirmada" || (b.Status == "Retenida" && b.ExpiresAt > now)),
            cancellationToken);

        if (isOccupied)
        {
            _logger.LogWarning("Seat {SeatId} in Event {EventId} is already occupied. Rejecting reservation {RequestId}.",
                request.SeatId, request.EventId, request.RequestId);

            var rejectedBooking = new BookingEntity
            {
                Id = request.RequestId,
                EventId = request.EventId,
                SeatId = request.SeatId,
                UserId = request.UserId ?? "anonymous",
                Status = "Rechazada",
                RejectionReason = "Asiento ocupado",
                ExpiresAt = now,
                CreatedAt = now
            };

            _context.Bookings.Add(rejectedBooking);
            await _context.SaveChangesAsync(cancellationToken);
            return rejectedBooking;
        }

        // 4. Seat free: Attempt to create retention
        _logger.LogInformation("Seat {SeatId} is available. Creating 'Retenida' booking for RequestId {RequestId}.",
            request.SeatId, request.RequestId);

        var booking = new BookingEntity
        {
            Id = request.RequestId,
            EventId = request.EventId,
            SeatId = request.SeatId,
            UserId = request.UserId ?? "anonymous",
            Status = "Retenida",
            ExpiresAt = now.AddMinutes(2), // 2-minute retention window
            CreatedAt = now
        };

        try
        {
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
        {
            // Concurrency race condition detected: another replica reserved the seat at the exact same millisecond.
            // The PostgreSQL partial unique index IX_bookings_EventId_SeatId_Active caught the collision atomically.
            _logger.LogWarning("Database partial unique constraint caught concurrent booking collision for Seat {SeatId}. Rejecting RequestId {RequestId}.",
                request.SeatId, request.RequestId);

            _context.Entry(booking).State = EntityState.Detached;

            var rejectedBooking = new BookingEntity
            {
                Id = request.RequestId,
                EventId = request.EventId,
                SeatId = request.SeatId,
                UserId = request.UserId ?? "anonymous",
                Status = "Rechazada",
                RejectionReason = "Asiento ocupado",
                ExpiresAt = now,
                CreatedAt = now
            };

            _context.Bookings.Add(rejectedBooking);
            await _context.SaveChangesAsync(cancellationToken);
            return rejectedBooking;
        }

        // 5. Flow step 5: Call Payment service via gRPC
        _logger.LogInformation("Initiating payment for booking {BookingId}...", booking.Id);
        var paymentResult = await _paymentServiceClient.ProcessPaymentAsync(
            booking.Id, 
            booking.UserId, 
            15000.00m,
            cancellationToken);

        // 6. Flow step 6: Update status based on payment outcome
        if (paymentResult.IsSuccess)
        {
            booking.Status = "Confirmada";
            booking.RejectionReason = null;
            _logger.LogInformation("Payment successful. Booking {BookingId} CONFIRMED.", booking.Id);
        }
        else
        {
            booking.Status = "Rechazada";
            booking.RejectionReason = paymentResult.ErrorMessage ?? "Pago fallido";
            _logger.LogWarning("Payment rejected. Booking {BookingId} REJECTED. Reason: {Reason}", booking.Id, booking.RejectionReason);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return booking;
    }
}
