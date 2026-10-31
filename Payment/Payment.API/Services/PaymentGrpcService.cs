using Grpc.Core;
using Microsoft.Extensions.Logging;
using Payment.API.Protos;
using Payment.BLL.Interfaces;
using DomainPaymentStatus = Payment.Domain.Enums.PaymentStatus;

namespace Payment.API.Services;

// Clase que adapta el contrato gRPC/protobuf a la interfaz que entiende el dominio
// interno (IPaymentService, PaymentAttempt) - (Federico Diaz)
public class PaymentGrpcService : Protos.PaymentService.PaymentServiceBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentGrpcService> _logger;

    // Se pide IPaymentService por constructor y Program.cs resuelve que implementacion inyectar
    // Queda desacoplado de los detalles de las capas BLL/DAL - (Federico Diaz)
    public PaymentGrpcService(IPaymentService paymentService, ILogger<PaymentGrpcService> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    // Metodo que recibe el request de booking, delega la logica del procesamiento de pago en PaymentService (BLL)
    // y devuelve un ProcessPaymentResponse segun el contrato definido en .proto - (Federico Diaz)
    public override async Task<ProcessPaymentResponse> ProcessPayment(ProcessPaymentRequest request, ServerCallContext context)
    {
        _logger.LogInformation(
            "Solicitud de pago recibida. BookingId={BookingId} UserId={UserId} Amount={Amount} Currency={Currency}",
            request.BookingId, request.UserId, request.Amount, request.Currency);

        var attempt = await _paymentService.ProcessPaymentAsync(
            request.BookingId,
            request.UserId,
            (decimal)request.Amount,
            request.Currency);

        var response = new ProcessPaymentResponse
        {
            Status = MapStatus(attempt.Status),
            TransactionId = attempt.Id.ToString(),
            ErrorMessage = MapErrorMessage(attempt.Status)
        };

        _logger.LogInformation(
            "Respuesta de pago enviada. BookingId={BookingId} TransactionId={TransactionId} Status={Status}",
            request.BookingId, response.TransactionId, response.Status);

        return response;
    }

    // Mapper que traduce el enum de dominio (Payment.Domain.Enums.PaymentStatus) al enum generado
    // por protobuf (Payment.API.Protos.PaymentStatus) - (Federico Diaz)
    private static Protos.PaymentStatus MapStatus(DomainPaymentStatus status) => status switch
    {
        DomainPaymentStatus.Success => Protos.PaymentStatus.Success,
        DomainPaymentStatus.Declined => Protos.PaymentStatus.Declined,
        DomainPaymentStatus.Unavailable => Protos.PaymentStatus.Unavailable,
        _ => Protos.PaymentStatus.Unspecified
    };
    
    // Mapper que arma el texto de error_message en base al status del PaymentAttempt - (Federico Diaz)
    private static string MapErrorMessage(DomainPaymentStatus status) => status switch
    {
        DomainPaymentStatus.Success => string.Empty,
        DomainPaymentStatus.Declined => "El pago fue rechazado.",
        DomainPaymentStatus.Unavailable => "El servicio de pago no está disponible.",
        _ => "No se pudo determinar el resultado del pago."
    };
}
