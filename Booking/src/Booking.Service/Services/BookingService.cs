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
using Booking.Service.Interfaces;

namespace Booking.Service.Services;

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

    public async Task<BookingEntity?> GetBookingByIdAsync(Guid id)
    {
        return await _context.Bookings.FindAsync(id);
    }

    // Franco Brienza: occupied-seat lookup.
    public async Task<List<string>> GetOccupiedSeatsAsync(string eventId)
    {
        var now = DateTime.UtcNow;

        return await _context.Bookings
            .Where(b =>
                b.EventId == eventId
                && (
                    b.Status == BookingStatus.Confirmada
                    || (
                        b.Status == BookingStatus.Retenida
                        && b.ExpiresAt > now
                    )
                ))
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
            .Where(b =>
                b.EventId == eventId
                && b.SeatId == seatId
                && b.Status == BookingStatus.Retenida
                && b.ExpiresAt <= now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    b => b.Status,
                    BookingStatus.Vencida),
                cancellationToken);
    }

    public async Task<int> ExpireRetentionsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Bookings
            .Where(booking =>
                booking.Status == BookingStatus.Retenida &&
                booking.ExpiresAt <= now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    booking => booking.Status,
                    BookingStatus.Vencida),
                cancellationToken);
    }

    public async Task<BookingEntity> ProcessReservationRequestAsync(
        ReservationRequestedMessage request,
        CancellationToken cancellationToken = default)
    {
        // Return prior results so message retries remain idempotent.
        var existingBooking = await _context.Bookings
            .FirstOrDefaultAsync(
                b => b.Id == request.RequestId,
                cancellationToken);

        if (existingBooking != null)
        {
            _logger.LogInformation(
                "Request {RequestId} already processed with status " +
                "{Status}. Returning existing record.",
                request.RequestId,
                existingBooking.Status);

            return existingBooking;
        }

        // Free this seat now; the background worker handles other expired holds.
        var expiredCount = await ExpireRetentionsForSeatAsync(
            request.EventId,
            request.SeatId,
            cancellationToken);

        if (expiredCount > 0)
        {
            _logger.LogInformation(
                "Releasing {Count} expired retention(s) for Seat " +
                "{SeatId} in Event {EventId}.",
                expiredCount,
                request.SeatId,
                request.EventId);
        }

        var now = DateTime.UtcNow;

        // An unexpired hold and a confirmed booking both occupy the seat.
        var isOccupied = await _context.Bookings.AnyAsync(
            b =>
                b.EventId == request.EventId
                && b.SeatId == request.SeatId
                && (
                    b.Status == BookingStatus.Confirmada
                    || (
                        b.Status == BookingStatus.Retenida
                        && b.ExpiresAt > now
                    )
                ),
            cancellationToken);

        if (isOccupied)
        {
            _logger.LogWarning(
                "Seat {SeatId} in Event {EventId} is already occupied. " +
                "Rejecting reservation {RequestId}.",
                request.SeatId,
                request.EventId,
                request.RequestId);

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

        _logger.LogInformation(
            "Seat {SeatId} is available. Creating 'Retenida' booking " +
            "for RequestId {RequestId}.",
            request.SeatId,
            request.RequestId);

        var booking = new BookingEntity
        {
            Id = request.RequestId,
            EventId = request.EventId,
            SeatId = request.SeatId,
            UserId = request.UserId ?? "anonymous",
            Status = BookingStatus.Retenida,
            ExpiresAt = now.AddMinutes(2),
            CreatedAt = now
        };

        try
        {
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException pgEx
            && pgEx.SqlState == "23505")
        {
            // The partial unique index resolves concurrent reservation races.
            _logger.LogWarning(
                "Database partial unique constraint caught concurrent " +
                "booking collision for Seat {SeatId}. " +
                "Rejecting RequestId {RequestId}.",
                request.SeatId,
                request.RequestId);

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

        _logger.LogInformation(
            "Initiating payment for booking {BookingId}...",
            booking.Id);

        var paymentResult = await _paymentServiceClient.ProcessPaymentAsync(
            booking.Id,
            booking.UserId,
            request.Amount,
            request.Currency,
            cancellationToken);

        if (paymentResult.IsSuccess)
        {
            booking.Status = BookingStatus.Confirmada;
            booking.RejectionReason = null;

            _logger.LogInformation(
                "Payment successful. Booking {BookingId} CONFIRMED.",
                booking.Id);
        }
        else
        {
            booking.Status = BookingStatus.Rechazada;
            booking.RejectionReason =
                paymentResult.ErrorMessage ?? "Pago fallido";

            _logger.LogWarning(
                "Payment rejected. Booking {BookingId} REJECTED. " +
                "Reason: {Reason}",
                booking.Id,
                booking.RejectionReason);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return booking;
    }
}