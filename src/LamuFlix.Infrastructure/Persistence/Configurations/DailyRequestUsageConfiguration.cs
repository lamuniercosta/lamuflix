using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LamuFlix.Infrastructure.Persistence.Configurations;

public sealed class DailyRequestUsageConfiguration : IEntityTypeConfiguration<DailyRequestUsageRecord>
{
    public const string NonNegativeRequestCountConstraint = "ck_omdb_daily_usage_request_count_nonnegative";

    public void Configure(EntityTypeBuilder<DailyRequestUsageRecord> builder)
    {
        builder.ToTable(
            "omdb_daily_usage",
            table => table.HasCheckConstraint(
                NonNegativeRequestCountConstraint,
                "request_count >= 0"));
        builder.HasKey(usage => usage.UsageDate)
            .HasName("PK_omdb_daily_usage");
        builder.Property(usage => usage.UsageDate)
            .HasColumnName("usage_date")
            .HasColumnType("date")
            .ValueGeneratedNever()
            .IsRequired();
        builder.Property(usage => usage.RequestCount)
            .HasColumnName("request_count")
            .HasColumnType("integer")
            .IsRequired();
    }
}
