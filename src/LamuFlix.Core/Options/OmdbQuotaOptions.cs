using System.ComponentModel.DataAnnotations;

namespace LamuFlix.Core.Options;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// Bound from configuration by ServiceDefaultsExtensions.AddLamuFlixOptions
public sealed record OmdbQuotaOptions
{
    public const string SectionName = "Omdb:Quota";

    [Range(1, int.MaxValue)]
    public int DailyLimit { get; init; } = 1000;
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
