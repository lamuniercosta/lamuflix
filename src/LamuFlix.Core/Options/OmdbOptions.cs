using System.ComponentModel.DataAnnotations;

namespace LamuFlix.Core.Options;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// Bound from configuration by ServiceDefaultsExtensions.AddLamuFlixOptions
public sealed record OmdbOptions
{
    public const string SectionName = "Omdb";

    [Required]
    public string ApiKey { get; init; } = string.Empty;

    [Required]
    [Url]
    public string BaseUrl { get; init; } = string.Empty;
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
