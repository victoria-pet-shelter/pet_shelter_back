using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace main.Migrations
{
    /// <inheritdoc />
    public partial class AddModelRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Shelters_shelter_owner_id",
                table: "Shelters",
                column: "shelter_owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_Pets_breed_id",
                table: "Pets",
                column: "breed_id");

            migrationBuilder.CreateIndex(
                name: "IX_Pets_gender_id",
                table: "Pets",
                column: "gender_id");

            migrationBuilder.CreateIndex(
                name: "IX_Pets_shelter_id",
                table: "Pets",
                column: "shelter_id");

            migrationBuilder.CreateIndex(
                name: "IX_Pets_species_id",
                table: "Pets",
                column: "species_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Pets_Breeds_breed_id",
                table: "Pets",
                column: "breed_id",
                principalTable: "Breeds",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Pets_Genders_gender_id",
                table: "Pets",
                column: "gender_id",
                principalTable: "Genders",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Pets_Shelters_shelter_id",
                table: "Pets",
                column: "shelter_id",
                principalTable: "Shelters",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Pets_Species_species_id",
                table: "Pets",
                column: "species_id",
                principalTable: "Species",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Shelters_Users_shelter_owner_id",
                table: "Shelters",
                column: "shelter_owner_id",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pets_Breeds_breed_id",
                table: "Pets");

            migrationBuilder.DropForeignKey(
                name: "FK_Pets_Genders_gender_id",
                table: "Pets");

            migrationBuilder.DropForeignKey(
                name: "FK_Pets_Shelters_shelter_id",
                table: "Pets");

            migrationBuilder.DropForeignKey(
                name: "FK_Pets_Species_species_id",
                table: "Pets");

            migrationBuilder.DropForeignKey(
                name: "FK_Shelters_Users_shelter_owner_id",
                table: "Shelters");

            migrationBuilder.DropIndex(
                name: "IX_Shelters_shelter_owner_id",
                table: "Shelters");

            migrationBuilder.DropIndex(
                name: "IX_Pets_breed_id",
                table: "Pets");

            migrationBuilder.DropIndex(
                name: "IX_Pets_gender_id",
                table: "Pets");

            migrationBuilder.DropIndex(
                name: "IX_Pets_shelter_id",
                table: "Pets");

            migrationBuilder.DropIndex(
                name: "IX_Pets_species_id",
                table: "Pets");
        }
    }
}
