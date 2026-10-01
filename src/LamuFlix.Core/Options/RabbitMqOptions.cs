using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;

namespace LamuFlix.Core.Options;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// Bound from configuration by ServiceDefaultsExtensions.AddLamuFlixOptions
public sealed record RabbitMqOptions : IValidatableObject
{
    public const string SectionName = "RabbitMq";

    private const string RedactedSecret = "***";

    [Required]
    public string HostName { get; init; } = string.Empty;

    public int Port { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(30);

    [Range(1, ushort.MaxValue)]
    public ushort Prefetch { get; init; } = 1;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RetryDelay <= TimeSpan.Zero)
        {
            yield return RetryDelayResult();
        }
        else if (!IsIntegerMilliseconds(RetryDelay))
        {
            yield return RetryDelayResult();
        }
    }

#pragma warning disable IDE0051 // Called by the compiler-generated record ToString, which the analyzer cannot see.
    private bool PrintMembers(StringBuilder builder)
#pragma warning restore IDE0051
    {
        builder.Append("HostName = ").Append(HostName);
        builder.Append(", Port = ").Append(Port);
        builder.Append(", UserName = ").Append(UserName);
        builder.Append(", Password = ").Append(RedactedSecret);
        builder.Append(", RetryDelay = ").Append(RetryDelay);
        builder.Append(", Prefetch = ").Append(Prefetch);
        return true;
    }

    private static bool IsIntegerMilliseconds(TimeSpan retryDelay)
    {
        var milliseconds = retryDelay.TotalMilliseconds;
        return milliseconds >= 1 && milliseconds <= int.MaxValue;
    }

    private static ValidationResult RetryDelayResult() =>
        new(
            string.Format(
                CultureInfo.InvariantCulture,
                "RetryDelay must be positive and expressible as a whole number of milliseconds up to {0}.",
                int.MaxValue),
            [nameof(RetryDelay)]);
}
// ReSharper restore UnusedAutoPropertyAccessor.Global