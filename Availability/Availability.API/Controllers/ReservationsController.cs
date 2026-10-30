
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Availability.Application.Reservations.Request;

namespace Availability.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class ReservationsController : ControllerBase
    {
        private readonly RequestReservationUseCase _useCase;

        public ReservationsController(RequestReservationUseCase useCase)
        {
            _useCase = useCase;
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
        }
    }
}
