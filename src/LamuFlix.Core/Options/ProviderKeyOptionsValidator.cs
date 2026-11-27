using System;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Options;

public sealed class ProviderKeyOptionsValidator : IValidateOptions<ProviderKeyOptions>
{
    public ValidateOptionsResult Validate(string? name, ProviderKeyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.Equals(options.KeySet, ProviderKeyOptions.TestKeySet, StringComparison.OrdinalIgnoreCase)
            || string.Equals(options.KeySet, ProviderKeyOptions.ProdKeySet, StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Success;
        }

        return ValidateOptionsResult.Fail(
            $"{ProviderKeyOptions.SectionName}:{nameof(ProviderKeyOptions.KeySet)} must be '{ProviderKeyOptions.TestKeySet}' or '{ProviderKeyOptions.ProdKeySet}'.");
    }
}
