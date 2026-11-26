using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LamuFlix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutsideLibraryCandidateEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outside_library_candidate_evidence",
                columns: table => new
                {
                    tmdb_id = table.Column<int>(type: "integer", nullable: false),
                    imdb_id = table.Column<string>(type: "text", nullable: true),
                    collection_id = table.Column<int>(type: "integer", nullable: true),
                    rating = table.Column<decimal>(type: "numeric", nullable: true),
                    votes = table.Column<int>(type: "integer", nullable: true),
                    outcome = table.Column<string>(type: "text", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outside_library_candidate_evidence", x => x.tmdb_id);
                    table.CheckConstraint("ck_candidate_evidence_durable_outcome", "outcome in ('Complete', 'ConfirmedAbsence')");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outside_library_candidate_evidence");
        }
    }
}
