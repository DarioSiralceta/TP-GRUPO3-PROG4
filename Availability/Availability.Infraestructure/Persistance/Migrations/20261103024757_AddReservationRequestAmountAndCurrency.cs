using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Availability.Infraestructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationRequestAmountAndCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "ReservationRequests",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "ReservationRequests",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "ReservationRequests");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "ReservationRequests");
        }
    }
}
