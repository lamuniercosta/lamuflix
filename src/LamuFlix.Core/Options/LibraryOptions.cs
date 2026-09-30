using System.ComponentModel.DataAnnotations;

namespace LamuFlix.Core.Options;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// Bound from configuration by ServiceDefaultsExtensions.AddLamuFlixOptions
public sealed record LibraryOptions
{
    public const string SectionName = "Library";

    [Required]
    public string RootPath { get; init; } = string.Empty;
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
