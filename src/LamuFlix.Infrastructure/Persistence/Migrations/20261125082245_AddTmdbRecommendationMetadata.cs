using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LamuFlix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTmdbRecommendationMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "poster_path",
                table: "movie_recommendations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "release_year",
                table: "movie_recommendations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "title",
                table: "movie_recommendations",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "poster_path",
                table: "movie_recommendations");

            migrationBuilder.DropColumn(
                name: "release_year",
                table: "movie_recommendations");

            migrationBuilder.DropColumn(
                name: "title",
                table: "movie_recommendations");
        }
    }
}
