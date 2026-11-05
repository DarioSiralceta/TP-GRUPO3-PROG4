using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceBookingStatusEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_bookings_Status",
                table: "bookings",
                sql: "\"Status\" IN ('Retenida', 'Confirmada', 'Rechazada', 'Vencida')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_bookings_Status",
                table: "bookings");
        }
    }
}
