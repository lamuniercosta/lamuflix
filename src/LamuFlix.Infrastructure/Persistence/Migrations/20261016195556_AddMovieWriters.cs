using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LamuFlix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMovieWriters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "writers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_writers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "movie_writers",
                columns: table => new
                {
                    movie_id = table.Column<int>(type: "integer", nullable: false),
                    writer_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movie_writers", x => new { x.movie_id, x.writer_id });
                    table.ForeignKey(
                        name: "FK_movie_writers_movies_movie_id",
                        column: x => x.movie_id,
                        principalTable: "movies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_movie_writers_writers_writer_id",
                        column: x => x.writer_id,
                        principalTable: "writers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_movie_writers_writer_id",
                table: "movie_writers",
                column: "writer_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "movie_writers");

            migrationBuilder.DropTable(
                name: "writers");
        }
    }
}
