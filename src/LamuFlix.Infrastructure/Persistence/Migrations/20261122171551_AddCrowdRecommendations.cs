using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LamuFlix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCrowdRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "tmdb_id",
                table: "movies",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "movie_recommendations",
                columns: table => new
                {
                    source_movie_id = table.Column<int>(type: "integer", nullable: false),
                    recommended_tmdb_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movie_recommendations", x => new { x.source_movie_id, x.recommended_tmdb_id });
                    table.ForeignKey(
                        name: "FK_movie_recommendations_movies_source_movie_id",
                        column: x => x.source_movie_id,
                        principalTable: "movies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_movie_recommendations_recommended_tmdb_id",
                table: "movie_recommendations",
                column: "recommended_tmdb_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "movie_recommendations");

            migrationBuilder.DropColumn(
                name: "tmdb_id",
                table: "movies");
        }
    }
}
