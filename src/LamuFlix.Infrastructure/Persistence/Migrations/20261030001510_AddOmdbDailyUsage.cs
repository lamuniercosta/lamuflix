using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LamuFlix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOmdbDailyUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "omdb_daily_usage",
                columns: table => new
                {
                    usage_date = table.Column<DateOnly>(type: "date", nullable: false),
                    request_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_omdb_daily_usage", x => x.usage_date);
                    table.CheckConstraint("ck_omdb_daily_usage_request_count_nonnegative", "request_count >= 0");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "omdb_daily_usage");
        }
    }
}
