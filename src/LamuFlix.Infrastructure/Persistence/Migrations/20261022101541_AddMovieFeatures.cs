using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace LamuFlix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMovieFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "movie_features",
                columns: table => new
                {
                    movie_id = table.Column<int>(type: "integer", nullable: false),
                    vector = table.Column<Vector>(type: "vector(384)", nullable: false),
                    computed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movie_features", x => x.movie_id);
                    table.ForeignKey(
                        name: "FK_movie_features_movies_movie_id",
                        column: x => x.movie_id,
                        principalTable: "movies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """CREATE INDEX "IX_movie_features_vector" ON movie_features USING hnsw (vector vector_cosine_ops);""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "movie_features");
        }
    }
}
