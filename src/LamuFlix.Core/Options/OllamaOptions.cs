using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LamuFlix.Core.Options;

public sealed record OllamaOptions : IValidatableObject
{
    public const string SectionName = "Ollama";

    // ReSharper disable once UnusedAutoPropertyAccessor.Global // set via configuration binding.
    [Required]
    public string Endpoint { get; init; } = "http://localhost:11434/";

    // ReSharper disable once UnusedAutoPropertyAccessor.Global // set via configuration binding.
    [Required]
    public string ModelName { get; init; } = "all-minilm";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(Endpoint) && !IsAbsoluteHttpOrHttps(Endpoint))
        {
            yield return new ValidationResult(
                "The Endpoint field must be an absolute http or https URL.",
                [nameof(Endpoint)]);
        }
    }

    private static bool IsAbsoluteHttpOrHttps(string text) =>
        Uri.TryCreate(text, UriKind.Absolute, out Uri? uri)
        && (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
}
