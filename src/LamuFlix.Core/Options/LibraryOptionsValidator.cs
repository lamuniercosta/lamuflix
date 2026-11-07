using System;
using System.Collections.Generic;
using LamuFlix.Core.Domain;
using Microsoft.Extensions.Options;

namespace LamuFlix.Core.Options;

public sealed class LibraryOptionsValidator : IValidateOptions<LibraryOptions>
{
    private const string LegacyRootPathsKey =
        $"{LibraryOptions.SectionName}:{nameof(LibraryOptions.LegacyRootPaths)}";

    public ValidateOptionsResult Validate(string? name, LibraryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var failures = new List<string>();
        AddEntryFailures(options, failures);
        AddConflictFailures(options, failures);
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void AddEntryFailures(LibraryOptions options, List<string> failures)
    {
        for (var index = 0; index < options.LegacyRootPaths.Length; index++)
        {
            var entry = options.LegacyRootPaths[index];
            var key = $"{LegacyRootPathsKey}[{index}]";
            if (string.IsNullOrWhiteSpace(entry))
            {
                failures.Add($"{key} must not be blank. Remove the entry or give an absolute legacy library root.");
                continue;
            }

            if (!LibraryPath.IsAbsolute(entry))
            {
                failures.Add(
                    $"{key} '{entry}' is not an absolute library root. Use an absolute Windows drive or UNC root "
                    + $"(for example C:/library or //server/share/library) or an absolute POSIX root (for example /media/library).");
            }
        }
    }

    private static void AddConflictFailures(LibraryOptions options, List<string> failures)
    {
        var aliases = options.LegacyRootPaths;
        for (var index = 0; index < aliases.Length; index++)
        {
            for (var other = index + 1; other < aliases.Length; other++)
            {
                if (!LibraryPath.OverlapsAtSegmentBoundary(aliases[index], aliases[other]))
                {
                    continue;
                }

                failures.Add(
                    $"{LegacyRootPathsKey}[{index}] '{aliases[index]}' and [{other}] '{aliases[other]}' overlap, so one stored "
                    + "path could map to two different library-relative identities. Keep a single unambiguous legacy root for that location.");
            }
        }
    }
}
