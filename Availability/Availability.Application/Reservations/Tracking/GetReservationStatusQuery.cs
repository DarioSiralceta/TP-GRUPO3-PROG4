namespace Availability.Application.Reservations.Tracking;

public class GetReservationStatusQuery
{
    public Guid RequestId { get; set; }

    public string UserId { get; set; } = string.Empty;
}