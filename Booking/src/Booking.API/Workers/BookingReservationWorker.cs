// src/Booking.API/Workers/BookingReservationWorker.cs
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Booking.Infrastructure.Configuration;
using Booking.Service.DTOs;
using Booking.Service.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Booking.API.Workers;

/// <summary>
/// Background Worker (Hosted Service) en Booking API encargado de consumir las solicitudes de reserva
/// publicadas por el sitio Availability en Azure Service Bus (Topic: grupo3-booking-topic).
/// 
/// Decisiones arquitectónicas y patrones implementados:
/// - Competing Consumers Pattern: Al suscribirse todas las réplicas de Booking a la misma suscripción del Topic,
///   Azure Service Bus distribuye cada mensaje a una única réplica (Paso 4 del flujo: "Una de las réplicas de Booking toma el mensaje").
/// - Eventual Consistency: Permite que Availability responda de inmediato al cliente (Paso 3) y Booking procese
///   de forma asíncrona la validación y el cobro.
/// - Scoped Service Factory: Al ser un BackgroundService Singleton y DbContext/BookingService Scoped, se utiliza IServiceScopeFactory
///   para aislar el ciclo de vida por mensaje.
/// - Auditoría (Req 3f): Emite logs identificando el inicio [AUDIT_START] y fin [AUDIT_END] de la cadena de sucesos.
/// </summary>
public class BookingReservationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AzureServiceBusOptions _options;
    private readonly ILogger<BookingReservationWorker> _logger;
    private ServiceBusClient? _client;
    private ServiceBusProcessor? _processor;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BookingReservationWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<AzureServiceBusOptions> options,
        ILogger<BookingReservationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            _logger.LogError("La cadena de conexión de Azure Service Bus no está configurada.");
            return;
        }

        try
        {
            // Extraer el Topic Name si viene especificado como EntityPath en la Connection String
            var properties = ServiceBusConnectionStringProperties.Parse(_options.ConnectionString);
            var topicName = !string.IsNullOrWhiteSpace(properties.EntityPath)
                ? properties.EntityPath
                : _options.TopicName;

            var subscriptionName = _options.SubscriptionName;

            _logger.LogInformation("Iniciando BookingReservationWorker para Topic: '{Topic}' y Suscripción: '{Subscription}'",
                topicName, subscriptionName);

            // Inicializar cliente y procesador de Service Bus
            _client = new ServiceBusClient(_options.ConnectionString);
            _processor = _client.CreateProcessor(topicName, subscriptionName, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false, // Control explícito de confirmación (ACK / NACK)
                MaxConcurrentCalls = _options.MaxConcurrentCalls > 0 ? _options.MaxConcurrentCalls : 1
            });

            _processor.ProcessMessageAsync += MessageHandlerAsync;
            _processor.ProcessErrorAsync += ErrorHandlerAsync;

            await _processor.StartProcessingAsync(stoppingToken);

            // Mantener el worker activo hasta que se solicite la cancelación
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("BookingReservationWorker detenido por cancelación de servicio.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error fatal al inicializar BookingReservationWorker en Azure Service Bus.");
        }
    }

    /// <summary>
    /// Manejador de mensajes entrantes desde el Topic de Service Bus.
    /// </summary>
    private async Task MessageHandlerAsync(ProcessMessageEventArgs args)
    {
        var stopwatch = Stopwatch.StartNew();
        var rawBody = args.Message.Body.ToString();
        var messageId = args.Message.MessageId;

        // Req 3f: Log de auditoría para identificar inicio de la cadena de sucesos en Booking
        _logger.LogInformation(
            "[AUDIT_START] [Microservicio: Booking API] Mensaje recibido desde Service Bus. MessageId: {MessageId}, DeliveryCount: {DeliveryCount}, Timestamp: {Timestamp:O}",
            messageId, args.Message.DeliveryCount, DateTime.UtcNow);

        try
        {
            var request = JsonSerializer.Deserialize<ReservationRequestedMessage>(rawBody, _jsonOptions);

            if (request == null || request.RequestId == Guid.Empty)
            {
                _logger.LogWarning("[AUDIT_ERROR] Mensaje inválido recibido: '{RawBody}'. Enviando a Dead Letter Queue.", rawBody);
                await args.DeadLetterMessageAsync(args.Message, "InvalidPayload", "No se pudo deserializar el contenido de la solicitud.", args.CancellationToken);
                return;
            }

            _logger.LogInformation(
                "[AUDIT_CHAIN_PROGRESS] Procesando SolicitudId: {RequestId}, EventoId: {EventId}, AsientoId: {SeatId}, UsuarioId: {UserId}",
                request.RequestId, request.EventId, request.SeatId, request.UserId);

            // Crear un scope por cada mensaje para resolver DbContext y servicios scoped
            using (var scope = _scopeFactory.CreateScope())
            {
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                // Ejecuta la verificación de asiento y la llamada a cobro con Payment
                var result = await bookingService.ProcessReservationRequestAsync(request, args.CancellationToken);

                // Req 3f: Log de auditoría para identificar el fin exitoso de la cadena de sucesos
                _logger.LogInformation(
                    "[AUDIT_END] [Microservicio: Booking API] Cadena de reserva finalizada para RequestId: {RequestId}. Estado: {Status}, MotivoRechazo: {Reason}, Duración: {ElapsedMs}ms",
                    result.Id, result.Status, result.RejectionReason ?? "N/A", stopwatch.ElapsedMilliseconds);
            }

            // Confirmar mensaje procesado (ACK) para eliminarlo de la suscripción
            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AUDIT_ERROR] Error no controlado durante el procesamiento del mensaje {MessageId}. Intentos: {DeliveryCount}",
                messageId, args.Message.DeliveryCount);

            // Si se superó el límite de reintentos, mover a Dead Letter para no bloquear la cola
            if (args.Message.DeliveryCount >= 5)
            {
                _logger.LogError("[AUDIT_DEADLETTER] Mensaje {MessageId} excede reintentos máximos. Enviando a Dead Letter Queue.", messageId);
                await args.DeadLetterMessageAsync(args.Message, "ExceededRetries", ex.Message, args.CancellationToken);
            }
            else
            {
                // Abandonar mensaje para reintento automático por parte de Service Bus
                await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
            }
        }
    }

    /// <summary>
    /// Manejador de errores de transporte y conexión de Azure Service Bus.
    /// </summary>
    private Task ErrorHandlerAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception,
            "[AUDIT_SERVICEBUS_ERROR] Error en Azure Service Bus. EntityPath: {EntityPath}, Source: {ErrorSource}",
            args.EntityPath, args.ErrorSource);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deteniendo BookingReservationWorker...");

        if (_processor != null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
        }

        if (_client != null)
        {
            await _client.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}
