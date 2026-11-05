namespace Availability.Web.Models;

public class SeguimientoApiResponse
{
    public Guid RequestId { get; set; }
    public int EventId { get; set; }
    public int SeatId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
}