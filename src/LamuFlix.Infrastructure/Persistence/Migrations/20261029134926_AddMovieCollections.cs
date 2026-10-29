using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LamuFlix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMovieCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "collection_id",
                table: "movies",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "collections",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_collections", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_movies_collection_id",
                table: "movies",
                column: "collection_id");

            migrationBuilder.AddForeignKey(
                name: "FK_movies_collections_collection_id",
                table: "movies",
                column: "collection_id",
                principalTable: "collections",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_movies_collections_collection_id",
                table: "movies");

            migrationBuilder.DropTable(
                name: "collections");

            migrationBuilder.DropIndex(
                name: "IX_movies_collection_id",
                table: "movies");

            migrationBuilder.DropColumn(
                name: "collection_id",
                table: "movies");
        }
    }
}
