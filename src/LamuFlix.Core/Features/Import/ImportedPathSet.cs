using System;
using System.Collections.Generic;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Import;

internal static class ImportedPathSet
{
    internal static HashSet<string> FromPaths(IEnumerable<LibraryPath> paths)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths)
        {
            set.Add(path.Value);
        }

        return set;
    }
}
