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

namespace Booking.Service.Services;

public interface IBookingService
{
    Task<IEnumerable<BookingEntity>> GetAllBookingsAsync();
    Task<BookingEntity?> GetBookingByIdAsync(Guid id);
    Task<BookingEntity> CreateRetentionAsync(string eventId, string seatId, string userId);
    Task<BookingEntity> ProcessReservationRequestAsync(ReservationRequestedMessage request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementación del servicio de negocio para Booking siguiendo el patrón N-Layer y Dominio Anémico.
/// La lógica de negocio y orquestación del flujo de reservas reside en esta capa de servicio.
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
        
        // Evaluar reservas retenidas vencidas
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

        // Regla de negocio 8: Vencimiento de retención
        if (booking.Status == "Retenida" && booking.ExpiresAt <= DateTime.UtcNow)
        {
            booking.Status = "Vencida";
            await _context.SaveChangesAsync();
        }

        return booking;
    }
    
    public async Task<BookingEntity> CreateRetentionAsync(string eventId, string seatId, string userId)
    {
        var booking = new BookingEntity
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            SeatId = seatId,
            UserId = userId,
            Status = "Retenida",
            ExpiresAt = DateTime.UtcNow.AddMinutes(2),
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        return booking;
    }

    /// <summary>
    /// Procesa el mensaje recibido desde Availability a través de Azure Service Bus.
    /// Pasos del flujo:
    /// 4. Procesamiento: Verifica si el asiento está libre. Si está libre crea reserva "Retenida", si está ocupado la rechaza ("Rechazada").
    /// 5. Cobro: Llama por gRPC a Payment para cobrar la reserva retenida.
    /// 6. Resultado: Según la respuesta de Payment, Booking confirma ("Confirmada") o rechaza ("Rechazada").
    /// </summary>
    public async Task<BookingEntity> ProcessReservationRequestAsync(
        ReservationRequestedMessage request, 
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // 1. Idempotencia: Verificar si el RequestId ya fue procesado previamente
        var existingBooking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == request.RequestId, cancellationToken);
        if (existingBooking != null)
        {
            _logger.LogInformation("Solicitud {RequestId} ya existe con estado {Status}. Retornando entidad existente.",
                request.RequestId, existingBooking.Status);
            return existingBooking;
        }

        // 2. Verificar disponibilidad del asiento
        // Regla: "Un asiento está ocupado mientras tenga una reserva retenida o confirmada. En cualquier otro estado, el asiento vuelve a estar disponible."
        var isOccupied = await _context.Bookings.AnyAsync(b =>
            b.EventId == request.EventId &&
            b.SeatId == request.SeatId &&
            (b.Status == "Confirmada" || (b.Status == "Retenida" && b.ExpiresAt > now)),
            cancellationToken);

        if (isOccupied)
        {
            _logger.LogWarning("Asiento {SeatId} en Evento {EventId} ya está ocupado. Rechazando reserva {RequestId}.",
                request.SeatId, request.EventId, request.RequestId);

            var rejectedBooking = new BookingEntity
            {
                Id = request.RequestId,
                EventId = request.EventId,
                SeatId = request.SeatId,
                UserId = request.UserId ?? "anonimo",
                Status = "Rechazada",
                RejectionReason = "Asiento ocupado",
                ExpiresAt = now,
                CreatedAt = now
            };

            _context.Bookings.Add(rejectedBooking);
            await _context.SaveChangesAsync(cancellationToken);
            return rejectedBooking;
        }

        // 3. Asiento libre: Crear reserva en estado "Retenida"
        _logger.LogInformation("Asiento {SeatId} libre. Creando reserva Retenida para RequestId {RequestId}.",
            request.SeatId, request.RequestId);

        var booking = new BookingEntity
        {
            Id = request.RequestId,
            EventId = request.EventId,
            SeatId = request.SeatId,
            UserId = request.UserId ?? "anonimo",
            Status = "Retenida",
            ExpiresAt = now.AddMinutes(2), // Tiempo de retención para cobrar
            CreatedAt = now
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync(cancellationToken);

        // 4. Paso 5 del flujo: Llamar a Payment (vía gRPC) para cobrar la reserva retenida
        _logger.LogInformation("Iniciando cobro a Payment para reserva {BookingId}...", booking.Id);
        var paymentResult = await _paymentServiceClient.ProcessPaymentAsync(
            booking.Id, 
            booking.UserId, 
            15000.00m, // Monto estimado o configurable
            cancellationToken);

        // 5. Paso 6 del flujo: Actualizar resultado según cobro
        if (paymentResult.IsSuccess)
        {
            booking.Status = "Confirmada";
            booking.RejectionReason = null;
            _logger.LogInformation("Pago exitoso. Reserva {BookingId} CONFIRMADA.", booking.Id);
        }
        else
        {
            booking.Status = "Rechazada";
            booking.RejectionReason = paymentResult.ErrorMessage ?? "Pago fallido";
            _logger.LogWarning("Pago rechazado. Reserva {BookingId} RECHAZADA. Motivo: {Reason}", booking.Id, booking.RejectionReason);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return booking;
    }
}
