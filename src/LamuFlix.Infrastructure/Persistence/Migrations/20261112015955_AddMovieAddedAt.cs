using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LamuFlix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMovieAddedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "added_at",
                table: "movies",
                type: "timestamptz",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.Sql("ALTER TABLE movies ALTER COLUMN added_at DROP DEFAULT;");

            migrationBuilder.CreateIndex(
                name: "IX_movies_added_at",
                table: "movies",
                column: "added_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_movies_added_at",
                table: "movies");

            migrationBuilder.DropColumn(
                name: "added_at",
                table: "movies");
        }
    }
}
