using Microsoft.EntityFrameworkCore.Migrations;

namespace LamuFlix.Data.Migrations
{
    public partial class Modifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdImdb",
                table: "movie",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsInWatchList",
                table: "movie",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MetaScore",
                table: "movie",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RottenTomatoes",
                table: "movie",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdImdb",
                table: "movie");

            migrationBuilder.DropColumn(
                name: "IsInWatchList",
                table: "movie");

            migrationBuilder.DropColumn(
                name: "MetaScore",
                table: "movie");

            migrationBuilder.DropColumn(
                name: "RottenTomatoes",
                table: "movie");
        }
    }
}
