using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace main.Migrations
{
    /// <inheritdoc />
    public partial class AddPetCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_user_id",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Favorites_user_id",
                table: "Favorites");

            migrationBuilder.DropIndex(
                name: "IX_AdoptionRequests_user_id",
                table: "AdoptionRequests");

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "Pets",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_email",
                table: "Users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_name",
                table: "Users",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_Shelters_email",
                table: "Shelters",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_user_id_shelter_id",
                table: "Reviews",
                columns: new[] { "user_id", "shelter_id" });

            migrationBuilder.CreateIndex(
                name: "IX_Pets_external_url",
                table: "Pets",
                column: "external_url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_user_id_pet_id",
                table: "Favorites",
                columns: new[] { "user_id", "pet_id" });

            migrationBuilder.CreateIndex(
                name: "IX_AdoptionRequests_user_id_pet_id",
                table: "AdoptionRequests",
                columns: new[] { "user_id", "pet_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_name",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Shelters_email",
                table: "Shelters");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_user_id_shelter_id",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Pets_external_url",
                table: "Pets");

            migrationBuilder.DropIndex(
                name: "IX_Favorites_user_id_pet_id",
                table: "Favorites");

            migrationBuilder.DropIndex(
                name: "IX_AdoptionRequests_user_id_pet_id",
                table: "AdoptionRequests");

            migrationBuilder.DropColumn(
                name: "category",
                table: "Pets");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_user_id",
                table: "Reviews",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_user_id",
                table: "Favorites",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_AdoptionRequests_user_id",
                table: "AdoptionRequests",
                column: "user_id");
        }
    }
}
