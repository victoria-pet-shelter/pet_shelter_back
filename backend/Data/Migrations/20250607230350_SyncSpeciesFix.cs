using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace main.Migrations
{
    /// <inheritdoc />
    public partial class SyncSpeciesFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "id",
                keyValue: 1,
                column: "name",
                value: "Dog");

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "id",
                keyValue: 2,
                column: "name",
                value: "Cat");

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "id",
                keyValue: 3,
                column: "name",
                value: "Exotic");

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "id",
                keyValue: 4,
                column: "name",
                value: "Rodent");

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "id",
                keyValue: 5,
                column: "name",
                value: "Bird");

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "id",
                keyValue: 6,
                column: "name",
                value: "Fish");

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "id",
                keyValue: 7,
                column: "name",
                value: "Farm");

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "id",
                keyValue: 8,
                column: "name",
                value: "Reptile");

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "id",
                keyValue: 9,
                column: "name",
                value: "Unknown");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "id",
                keyValue: 1,
                column: "name",
                value: "dog");

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "id",
                keyValue: 2,
                column: "name",
                value: "cat");

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "id",
                keyValue: 3,
                column: "name",
                value: "rabbit");

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "id",
                keyValue: 4,
                column: "name",
                value: "bird");

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "id",
                keyValue: 5,
                column: "name",
                value: "rodent");

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "id",
                keyValue: 6,
                column: "name",
                value: "reptile");

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "id",
                keyValue: 7,
                column: "name",
                value: "horse");

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "id",
                keyValue: 8,
                column: "name",
                value: "fish");

            migrationBuilder.UpdateData(
                table: "Species",
                keyColumn: "id",
                keyValue: 9,
                column: "name",
                value: "exotic");
        }
    }
}
