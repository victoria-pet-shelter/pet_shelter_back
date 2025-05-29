using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace main.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Reviews_shelter_id",
                table: "Reviews",
                column: "shelter_id");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_user_id",
                table: "Reviews",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_pet_id",
                table: "Favorites",
                column: "pet_id");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_user_id",
                table: "Favorites",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_AdoptionRequests_pet_id",
                table: "AdoptionRequests",
                column: "pet_id");

            migrationBuilder.CreateIndex(
                name: "IX_AdoptionRequests_user_id",
                table: "AdoptionRequests",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_AdoptionRequests_Pets_pet_id",
                table: "AdoptionRequests",
                column: "pet_id",
                principalTable: "Pets",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AdoptionRequests_Users_user_id",
                table: "AdoptionRequests",
                column: "user_id",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Favorites_Pets_pet_id",
                table: "Favorites",
                column: "pet_id",
                principalTable: "Pets",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Favorites_Users_user_id",
                table: "Favorites",
                column: "user_id",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Shelters_shelter_id",
                table: "Reviews",
                column: "shelter_id",
                principalTable: "Shelters",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Users_user_id",
                table: "Reviews",
                column: "user_id",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdoptionRequests_Pets_pet_id",
                table: "AdoptionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AdoptionRequests_Users_user_id",
                table: "AdoptionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Favorites_Pets_pet_id",
                table: "Favorites");

            migrationBuilder.DropForeignKey(
                name: "FK_Favorites_Users_user_id",
                table: "Favorites");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Shelters_shelter_id",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Users_user_id",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_shelter_id",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_user_id",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Favorites_pet_id",
                table: "Favorites");

            migrationBuilder.DropIndex(
                name: "IX_Favorites_user_id",
                table: "Favorites");

            migrationBuilder.DropIndex(
                name: "IX_AdoptionRequests_pet_id",
                table: "AdoptionRequests");

            migrationBuilder.DropIndex(
                name: "IX_AdoptionRequests_user_id",
                table: "AdoptionRequests");
        }
    }
}
