using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LamuFlix.Infrastructure.Persistence.Configurations;

public sealed class CandidateEvidenceConfiguration : IEntityTypeConfiguration<CandidateEvidenceRecord>
{
    private const string DurableOutcomeConstraint = "ck_candidate_evidence_durable_outcome";

    public void Configure(EntityTypeBuilder<CandidateEvidenceRecord> builder)
    {
        builder.ToTable(
            "outside_library_candidate_evidence",
            table => table.HasCheckConstraint(
                DurableOutcomeConstraint,
                "outcome in ('Complete', 'ConfirmedAbsence')"));
        builder.HasKey(evidence => evidence.TmdbId)
            .HasName("PK_outside_library_candidate_evidence");
        builder.Property(evidence => evidence.TmdbId)
            .HasColumnName("tmdb_id")
            .ValueGeneratedNever()
            .IsRequired();
        builder.Property(evidence => evidence.ImdbId)
            .HasColumnName("imdb_id");
        builder.Property(evidence => evidence.CollectionId)
            .HasColumnName("collection_id");
        builder.Property(evidence => evidence.Rating)
            .HasColumnName("rating")
            .HasColumnType("numeric");
        builder.Property(evidence => evidence.Votes)
            .HasColumnName("votes");
        builder.Property(evidence => evidence.Outcome)
            .HasColumnName("outcome")
            .IsRequired();
        builder.Property(evidence => evidence.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}
