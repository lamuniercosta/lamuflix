using System;
using System.IO;

namespace LamuFlix.Evaluation.RealLibrary;

internal static class EvaluationPaths
{
    internal static string Root { get; } = DiscoverRoot();

    internal static string Resolve(string? declared, string defaultRelative)
    {
        if (!string.IsNullOrWhiteSpace(declared))
        {
            return ResolveRelative(declared);
        }

        return Path.GetFullPath(Path.Combine(Root, Normalize(defaultRelative)));
    }

    internal static string ResolveRelative(string path) =>
        Path.IsPathFullyQualified(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(Root, Normalize(path)));

    private static string Normalize(string relative) =>
        relative.Replace('/', Path.DirectorySeparatorChar);

    private static string DiscoverRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "harness.yml"))
                || File.Exists(Path.Combine(directory.FullName, "LamuFlix.sln"))
                || Directory.Exists(Path.Combine(directory.FullName, ".git"))
                || File.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
