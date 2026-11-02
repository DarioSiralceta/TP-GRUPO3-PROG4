using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueActiveBookingIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_bookings_EventId_SeatId_Active",
                table: "bookings",
                columns: new[] { "EventId", "SeatId" },
                unique: true,
                filter: "\"Status\" IN ('Retenida', 'Confirmada')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_bookings_EventId_SeatId_Active",
                table: "bookings");
        }
    }
}
