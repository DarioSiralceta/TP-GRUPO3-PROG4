
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Availability.Application.Reservations.Request;
using Azure.Messaging.ServiceBus;
using Availability.Application.Reservations.Tracking;

namespace Availability.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class ReservationsController : ControllerBase
    {
        private readonly RequestReservationUseCase _useCase;
        private readonly GetReservationStatusUseCase _trackingUseCase;
        private readonly ILogger<ReservationsController> _logger;

        public ReservationsController(
            RequestReservationUseCase useCase,
            ILogger<ReservationsController> logger,
            GetReservationStatusUseCase trackingUseCase)
        {
            _useCase = useCase;
            _logger = logger;
            _trackingUseCase = trackingUseCase;
        }

        [HttpPost]
        public async Task<IActionResult> RequestReservation(
            [FromBody] RequestReservationComand comand)
        {
            var userId = User.FindFirst("sub")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new
                {
                    message = "El token no contiene el identificador del usuario."
                });
            }

            try
            {
                var result = await _useCase.Execute(comand, userId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ServiceBusException ex)
            {
                _logger.LogError(
                    ex,
                    "Error al publicar la solicitud de reserva en Azure Service Bus.");

                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new { message = "El servicio de reservas no está disponible temporalmente." });
            }
        }

        [HttpGet("{requestId:guid}")]
        public async Task<IActionResult> GetReservationStatus(Guid requestId)
        {
            var userId = User.FindFirst("sub")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new
                {
                    message = "El token no contiene el identificador del usuario."
                });
            }

            try
            {
                var query = new GetReservationStatusQuery
                {
                    RequestId = requestId,
                    UserId = userId
                };

                var result = await _trackingUseCase.ExecuteAsync(query);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new { message = ex.Message });
            }
            catch (Exception ex) when (
                ex is HttpRequestException
                or TaskCanceledException
                or System.Text.Json.JsonException
                or InvalidOperationException)
            {
                _logger.LogError(
                    ex,
                    "Error consultando el estado de la solicitud {RequestId}",
                    requestId);

                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        message = "El servicio de reservas no está disponible temporalmente."
                    });
            }
        }
    }
}
