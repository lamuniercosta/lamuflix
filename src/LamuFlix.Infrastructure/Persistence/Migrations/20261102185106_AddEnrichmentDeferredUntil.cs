using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LamuFlix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnrichmentDeferredUntil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "enrichment_deferred_until",
                table: "movies",
                type: "timestamptz",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "enrichment_deferred_until",
                table: "movies");
        }
    }
}
