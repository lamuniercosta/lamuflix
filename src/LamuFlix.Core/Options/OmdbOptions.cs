using System;
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
    [AllowedBaseUrl]
    public string BaseUrl { get; init; } = string.Empty;

    [AttributeUsage(AttributeTargets.Property)]
    private sealed class AllowedBaseUrlAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string text || text.Length == 0 || IsAllowed(text))
            {
                return ValidationResult.Success;
            }

            return new ValidationResult(
                "The BaseUrl field must be an absolute https URL, or http on a loopback host.",
                [validationContext.MemberName ?? nameof(BaseUrl)]);
        }

        private static bool IsAllowed(string text) =>
            Uri.TryCreate(text, UriKind.Absolute, out Uri? uri) && IsHttpsOrLoopbackHttp(uri);

        private static bool IsHttpsOrLoopbackHttp(Uri uri) =>
            uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) && uri.IsLoopback);
    }
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
