using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.Persistence;

public sealed class EfMetadataRequestGate(
    LamuFlixDbContext db,
    TimeProvider timeProvider,
    IOptions<OmdbQuotaOptions> quotaOptions) : IOmdbRequestGate
{
    public async Task<bool> TryAcquireAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var usageDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var dailyLimit = quotaOptions.Value.DailyLimit;

        var admitted = await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO omdb_daily_usage AS usage (usage_date, request_count)
            VALUES ({usageDate}, 1)
            ON CONFLICT (usage_date) DO UPDATE
            SET request_count = usage.request_count + 1
            WHERE usage.request_count < {dailyLimit}
            """,
            cancellationToken);

        return admitted == 1;
    }
}
