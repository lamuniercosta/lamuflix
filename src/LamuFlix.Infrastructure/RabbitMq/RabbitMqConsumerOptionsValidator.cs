using System;
using System.Collections.Generic;
using System.Globalization;
using LamuFlix.Core.Options;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.RabbitMq;

public sealed class RabbitMqConsumerOptionsValidator(IOptions<EnrichmentOptions> enrichment) : IValidateOptions<RabbitMqOptions>
{
    public ValidateOptionsResult Validate(string? name, RabbitMqOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var lease = enrichment.Value.ClaimLease;
        if (lease <= TimeSpan.Zero)
        {
            return Failure("Enrichment:ClaimLease must be explicitly configured and greater than zero when the consumer is active.");
        }

        if (!TryConvert(options.RetryDelay, out var ttl))
        {
            return Failure(
                $"RabbitMq:RetryDelay of {options.RetryDelay} must be a positive whole number of milliseconds no greater than {int.MaxValue}.");
        }

        return ttl > lease.TotalMilliseconds
            ? ValidateOptionsResult.Success
            : Failure(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "RabbitMq:RetryDelay of {0}ms must be strictly greater than Enrichment:ClaimLease of {1}ms, so a retry cannot arrive while the claim lease is still held.",
                    ttl,
                    lease.TotalMilliseconds));
    }

    private static bool TryConvert(TimeSpan retryDelay, out int milliseconds)
    {
        var value = retryDelay.TotalMilliseconds;
        if (value is < 1 or > int.MaxValue)
        {
            milliseconds = 0;
            return false;
        }

        milliseconds = checked((int)Math.Round(value, MidpointRounding.AwayFromZero));
        return milliseconds > 0;
    }

    private static ValidateOptionsResult Failure(string message) => ValidateOptionsResult.Fail(message);
}
