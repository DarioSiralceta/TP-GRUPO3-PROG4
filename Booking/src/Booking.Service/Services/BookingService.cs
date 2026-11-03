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

    Task<SeatStatusDto> GetSeatStatusAsync(
        string eventId,
        string seatId);

    Task<List<string>> GetOccupiedSeatsAsync(string eventId);

    Task<BookingEntity> ProcessReservationRequestAsync(
        ReservationRequestedMessage request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Servicio de Booking con arquitectura N-Layer y dominio anémico.
/// Gestiona disponibilidad, retenciones y procesamiento del pago.
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

    public async Task<SeatStatusDto> GetSeatStatusAsync(
        string eventId,
        string seatId)
    {
        var now = DateTime.UtcNow;

        var booking = await _context.Bookings
            .Where(b =>
                b.EventId == eventId
                && b.SeatId == seatId
                && (
                    b.Status == BookingStatus.Confirmada
                    || (
                        b.Status == BookingStatus.Retenida
                        && b.ExpiresAt > now
                    )
                ))
            .OrderByDescending(b =>
                b.Status == BookingStatus.Confirmada)
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

    public async Task<BookingEntity> ProcessReservationRequestAsync(
        ReservationRequestedMessage request,
        CancellationToken cancellationToken = default)
    {
        // Evitamos procesar nuevamente una solicitud ya registrada.
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

        // Liberamos las retenciones vencidas de este asiento.
        // El worker de vencimiento se ocupa de los demás asientos.
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

        // Un asiento está ocupado si tiene una reserva confirmada
        // o una retención que todavía no venció.
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
            // La restricción única de PostgreSQL detecta una colisión
            // cuando dos solicitudes intentan ocupar el mismo asiento.
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

        // Importe fijo conservado de la implementación recibida.
        // Pendiente: integrar el importe y la moneda de Availability.
        var paymentResult = await _paymentServiceClient.ProcessPaymentAsync(
            booking.Id,
            booking.UserId,
            15000.00m,
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