using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace main.Migrations
{
    /// <inheritdoc />
    public partial class AddUnknownSpecie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "INSERT INTO \"Species\" (\"id\", \"name\") VALUES (999, 'Unknown') " +
                "ON CONFLICT (\"id\") DO NOTHING;"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"Species\" WHERE \"id\" = 999;");
        }
    }
}
