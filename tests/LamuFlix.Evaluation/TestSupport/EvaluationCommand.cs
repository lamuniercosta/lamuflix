using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace LamuFlix.Evaluation.TestSupport;

internal sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError)
{
    public string Output => StandardOutput + StandardError;

    public string Describe() =>
        "exit code " + ExitCode.ToString(System.Globalization.CultureInfo.InvariantCulture)
        + Environment.NewLine + "stdout:" + Environment.NewLine + StandardOutput
        + Environment.NewLine + "stderr:" + Environment.NewLine + StandardError;
}

internal static class EvaluationCommand
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(15);

    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    private static string ScriptPath => Path.Combine(RepositoryRoot, "scripts", "run-evaluation.ps1");

    public static string FixturePath =>
        Path.Combine(RepositoryRoot, "tests", "LamuFlix.Evaluation", "Fixtures", "baseline.json");

    public static string DefaultOutputPath =>
        Path.Combine(RepositoryRoot, "artifacts", "evaluation", "metrics-summary.json");

    public static CommandResult Run(string workingDirectory, params string[] arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
        ArgumentNullException.ThrowIfNull(arguments);

        var startInfo = new ProcessStartInfo("pwsh")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(ScriptPath);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("pwsh could not be started.");
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(Timeout))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("scripts/run-evaluation.ps1 did not finish within " + Timeout);
        }

        return new CommandResult(
            process.ExitCode,
            standardOutput.GetAwaiter().GetResult(),
            standardError.GetAwaiter().GetResult());
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "LamuFlix.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the repository root (LamuFlix.sln) above " + AppContext.BaseDirectory);
    }
}
