using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LamuFlix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManualImdbIdUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_movies_manual_imdb_id",
                table: "movies",
                column: "manual_imdb_id",
                unique: true,
                filter: "manual_imdb_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_movies_manual_imdb_id",
                table: "movies");
        }
    }
}
