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
    Task<SeatStatusDto> GetSeatStatusAsync(string eventId, string seatId); // Franco Brienza - Copilot
    Task<List<string>> GetOccupiedSeatsAsync(string eventId); // Franco Brienza - Copilot
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
        return await _context.Bookings.ToListAsync();
    }

    public async Task<BookingEntity?> GetBookingByIdAsync(Guid id)
    {
        return await _context.Bookings.FindAsync(id);
    }

    public async Task<SeatStatusDto> GetSeatStatusAsync(string eventId, string seatId) // Franco Brienza - Copilot
    {
        var now = DateTime.UtcNow;
        var booking = await _context.Bookings
            .Where(b => b.EventId == eventId
                    && b.SeatId == seatId
                    && (b.Status == BookingStatus.Confirmada || (b.Status == BookingStatus.Retenida && b.ExpiresAt > now)))
            .OrderByDescending(b => b.Status == BookingStatus.Confirmada)
            .ThenByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync();

        return new SeatStatusDto
        {
            EventId = eventId,
            SeatId = seatId,
            IsOccupied = booking != null,
            Status = booking?.Status.ToString() ?? "Disponible"
        };
    }

    public async Task<List<string>> GetOccupiedSeatsAsync(string eventId) // Franco Brienza - Copilot
    {
        var now = DateTime.UtcNow;
        return await _context.Bookings
            .Where(b => b.EventId == eventId
                    && (b.Status == BookingStatus.Confirmada || (b.Status == BookingStatus.Retenida && b.ExpiresAt > now)))
            .Select(b => b.SeatId)
            .Distinct()
            .OrderBy(seatId => seatId)
            .ToListAsync();
    }

    private Task<int> ExpireRetentionsForSeatAsync(
        string eventId,
        string seatId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return _context.Bookings
            .Where(b => b.EventId == eventId
                    && b.SeatId == seatId
                    && b.Status == BookingStatus.Retenida
                    && b.ExpiresAt <= now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(b => b.Status, BookingStatus.Vencida),
                cancellationToken);
    }

    public async Task<BookingEntity> CreateRetentionAsync(string eventId, string seatId, string userId)
    {
        await ExpireRetentionsForSeatAsync(eventId, seatId);
        var now = DateTime.UtcNow;

        var booking = new BookingEntity
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            SeatId = seatId,
            UserId = userId,
            Status = BookingStatus.Retenida,
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
        // 1. Idempotency: Verify whether RequestId has already been processed
        var existingBooking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == request.RequestId, cancellationToken);
        if (existingBooking != null)
        {
            _logger.LogInformation("Request {RequestId} already processed with status {Status}. Returning existing record.",
                request.RequestId, existingBooking.Status);
            return existingBooking;
        }

        // Release an expired hold immediately; the background worker handles other seats.
        var expiredCount = await ExpireRetentionsForSeatAsync(
            request.EventId,
            request.SeatId,
            cancellationToken);
        if (expiredCount > 0)
        {
            _logger.LogInformation("Releasing {Count} expired retention(s) for Seat {SeatId} in Event {EventId}.",
                expiredCount, request.SeatId, request.EventId);
        }

        var now = DateTime.UtcNow;
        // 3. Application-level check: Verify seat availability
        // Rule: "A seat is occupied while it has a Retenida (unexpired) or Confirmada booking."
        var isOccupied = await _context.Bookings.AnyAsync(b =>
            b.EventId == request.EventId &&
            b.SeatId == request.SeatId &&
            (b.Status == BookingStatus.Confirmada || (b.Status == BookingStatus.Retenida && b.ExpiresAt > now)),
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
                Status = BookingStatus.Rechazada,
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
            Status = BookingStatus.Retenida,
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
                Status = BookingStatus.Rechazada,
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
            booking.Status = BookingStatus.Confirmada;
            booking.RejectionReason = null;
            _logger.LogInformation("Payment successful. Booking {BookingId} CONFIRMED.", booking.Id);
        }
        else
        {
            booking.Status = BookingStatus.Rechazada;
            booking.RejectionReason = paymentResult.ErrorMessage ?? "Pago fallido";
            _logger.LogWarning("Payment rejected. Booking {BookingId} REJECTED. Reason: {Reason}", booking.Id, booking.RejectionReason);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return booking;
    }
}
