using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Availability.Infraestructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventPriceAndCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Events",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Events",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Currency", "Price" },
                values: new object[] { "ARS", 20000m });

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Currency", "Price" },
                values: new object[] { "ARS", 30000m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Events");
        }
    }
}
