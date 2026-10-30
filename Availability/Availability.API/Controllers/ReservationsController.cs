
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Availability.Application.Reservations.Request;
using Azure.Messaging.ServiceBus;

namespace Availability.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class ReservationsController : ControllerBase
    {
        private readonly RequestReservationUseCase _useCase;
        private readonly ILogger<ReservationsController> _logger;

        public ReservationsController(
            RequestReservationUseCase useCase,
            ILogger<ReservationsController> logger)
        {
            _useCase = useCase;
            _logger = logger;
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
    }
}
