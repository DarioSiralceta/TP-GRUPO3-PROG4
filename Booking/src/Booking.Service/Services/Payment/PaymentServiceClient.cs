using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Payment.API.Protos;
using Shared.Authentication.Services;

namespace Booking.Service.Services.Payment;

/// <summary>
/// Cliente gRPC para invocar al microservicio Payment.
/// </summary>
public class PaymentServiceClient : IPaymentServiceClient
{
    private readonly ILogger<PaymentServiceClient> _logger;
    private readonly IConfiguration _configuration;
    private readonly KeycloakTokenService _keycloakTokenService;
    private readonly PaymentService.PaymentServiceClient _paymentClient;

    public PaymentServiceClient(
        ILogger<PaymentServiceClient> logger,
        IConfiguration configuration,
        KeycloakTokenService keycloakTokenService,
        PaymentService.PaymentServiceClient paymentClient)
    {
        _logger = logger;
        _configuration = configuration;
        _keycloakTokenService = keycloakTokenService;
        _paymentClient = paymentClient;
    }

    public async Task<PaymentResult> ProcessPaymentAsync( // Franco Brienza
        Guid bookingId,
        string userId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[AUDIT_PAYMENT_START] Solicitando cobro a Payment para BookingId: {BookingId}, Usuario: {UserId}, Monto: {Amount}",
            bookingId,
            userId,
            amount);

        var authority = _configuration["Keycloak:Authority"];
        var clientId = _configuration["Keycloak:ClientId"];
        var clientSecret = _configuration["Keycloak:ClientSecret"];

        if (string.IsNullOrWhiteSpace(authority)
            || string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(clientSecret))
        {
            _logger.LogError("No están configuradas las credenciales de servicio de Keycloak para invocar Payment.");
            return PaymentUnavailable();
        }

        try
        {
            var accessToken = await _keycloakTokenService.GetAccessTokenAsync(
                authority,
                clientId,
                clientSecret,
                cancellationToken);

            var headers = new Metadata
            {
                { "Authorization", $"Bearer {accessToken}" }
            };

            var request = new ProcessPaymentRequest
            {
                BookingId = bookingId.ToString(),
                UserId = userId,
                Amount = (double)amount,
                Currency = "ARS"
            };

            using var call = _paymentClient.ProcessPaymentAsync(
                request,
                headers,
                cancellationToken: cancellationToken);
            var response = await call.ResponseAsync;

            switch (response.Status)
            {
                case PaymentStatus.Success:
                    _logger.LogInformation(
                        "[AUDIT_PAYMENT_END] Cobro exitoso registrado en Payment. TxId: {TxId} para BookingId: {BookingId}",
                        response.TransactionId,
                        bookingId);
                    return new PaymentResult(true, TransactionId: response.TransactionId);

                case PaymentStatus.Declined:
                    _logger.LogWarning(
                        "[AUDIT_PAYMENT_FAILED] Payment rechazó el cobro para BookingId: {BookingId}",
                        bookingId);
                    return new PaymentResult(false, "Pago rechazado");

                case PaymentStatus.Unavailable:
                default:
                    _logger.LogWarning(
                        "[AUDIT_PAYMENT_UNAVAILABLE] Payment no está disponible para BookingId: {BookingId}. Estado recibido: {Status}",
                        bookingId,
                        response.Status);
                    return PaymentUnavailable();
            }
        }
        catch (RpcException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                exception,
                "[AUDIT_PAYMENT_UNAVAILABLE] Falló la llamada gRPC a Payment para BookingId: {BookingId}",
                bookingId);
            return PaymentUnavailable();
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "[AUDIT_PAYMENT_UNAVAILABLE] No se pudo obtener el token de Keycloak para BookingId: {BookingId}",
                bookingId);
            return PaymentUnavailable();
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogError(
                exception,
                "[AUDIT_PAYMENT_UNAVAILABLE] No se pudo preparar la llamada a Payment para BookingId: {BookingId}",
                bookingId);
            return PaymentUnavailable();
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                exception,
                "[AUDIT_PAYMENT_UNAVAILABLE] La llamada a Payment excedió el tiempo de espera para BookingId: {BookingId}",
                bookingId);
            return PaymentUnavailable();
        }
    }

    private static PaymentResult PaymentUnavailable() => new(false, "Pago no disponible");
}
