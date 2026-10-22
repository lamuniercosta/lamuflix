using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Adapters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class OllamaFixture : IAsyncLifetime
{
    private const string OptInVariable = "LAMUFLIX_RUN_OLLAMA_TESTS";
    private const string ModelName = "all-minilm";

    private const string Image =
        "ollama/ollama:0.40.8@sha256:2b8eecdb85aa6ea67c381c8ecffec04b2658496cf2821a8f88b0485bdff198e8";

    private const int OllamaPort = 11434;

    private static readonly TimeSpan ReadinessBudget = TimeSpan.FromSeconds(120);
    private static readonly TimeSpan PullBudget = TimeSpan.FromSeconds(900);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan CleanupBudget = TimeSpan.FromSeconds(60);

    private IContainer? container;

    public static string SkipReason =>
        FormattableString.Invariant(
            $"{OptInVariable}=1 must be set to run the real-model Ollama proof.");

    public static bool IsOptedIn =>
        Environment.GetEnvironmentVariable(OptInVariable) == "1";

    public static TimeSpan GenerationBudget => TimeSpan.FromSeconds(120);

    private Uri Endpoint =>
        container is null
            ? throw new InvalidOperationException("OllamaFixture has not been initialized.")
            : new Uri(
                string.Concat(
                    "http://",
                    container.Hostname,
                    ":",
                    container.GetMappedPublicPort(OllamaPort).ToString(CultureInfo.InvariantCulture),
                    "/"),
                UriKind.Absolute);

    public async ValueTask InitializeAsync()
    {
        if (!IsOptedIn)
        {
            return;
        }

        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);

        try
        {
            container = new ContainerBuilder(Image)
                .WithImage(Image)
                .WithPortBinding(OllamaPort, true)
                .WithCleanUp(true)
                .Build();

            await StartContainerAsync(container, cancellation.Token);
            await WaitForServerAsync(container, cancellation.Token);
            await PullModelAsync(container, cancellation.Token);
        }
        catch
        {
            await CleanupAsync();
            throw;
        }
    }

    public ValueTask DisposeAsync() => CleanupAsync();

    public ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        IOptions<OllamaOptions> ollamaOptions = Options.Create(new OllamaOptions
        {
            Endpoint = Endpoint.ToString(),
            ModelName = ModelName,
        });
        services.AddSingleton(ollamaOptions);
        IOptions<OllamaResilienceOptions> resilienceOptions =
            Options.Create(GenerationResilience());
        services.AddSingleton(resilienceOptions);
        services.AddOllamaEmbeddingGenerator();
        return services.BuildServiceProvider();
    }

    private static OllamaResilienceOptions GenerationResilience() => new()
    {
        AttemptTimeout = GenerationBudget,
        TotalTimeout = GenerationBudget,
        SamplingDuration = TimeSpan.FromSeconds(240),
        BreakDuration = TimeSpan.FromSeconds(5),
    };

    private static async Task StartContainerAsync(IContainer target, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(ReadinessBudget);

        try
        {
            await target.StartAsync(deadline.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException(
                FormattableString.Invariant(
                    $"The Ollama container did not start within {ReadinessBudget.TotalSeconds} seconds."),
                new TimeoutException());
        }
    }

    private static async Task WaitForServerAsync(IContainer target, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(ReadinessBudget);

        while (!deadline.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await ProbeReadyAsync(target, deadline.Token, cancellationToken))
            {
                return;
            }

            if (deadline.IsCancellationRequested)
            {
                break;
            }

            if (!await DelayBetweenProbesAsync(deadline.Token, cancellationToken))
            {
                break;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        var logs = await ReadLogsAsync(target, cancellationToken);
        throw new InvalidOperationException(
            FormattableString.Invariant(
                $"The Ollama server was not ready within {ReadinessBudget.TotalSeconds} seconds. Logs: {logs}"),
            new TimeoutException());
    }

    private static async Task<bool> ProbeReadyAsync(IContainer target, CancellationToken deadlineToken, CancellationToken cancellationToken)
    {
        try
        {
            var probe = await target.ExecAsync(["ollama", "list"], deadlineToken);
            return probe.ExitCode == 0;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private static async Task<bool> DelayBetweenProbesAsync(CancellationToken deadlineToken, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(PollInterval, deadlineToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private static async Task PullModelAsync(IContainer target, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(PullBudget);

        ExecResult pull;
        try
        {
            pull = await target.ExecAsync(["ollama", "pull", ModelName], deadline.Token);
        }
        catch (Exception failure) when (failure is not OperationCanceledException
                                        || !cancellationToken.IsCancellationRequested)
        {
            var logs = await ReadLogsAsync(target, cancellationToken);
            throw new InvalidOperationException(
                FormattableString.Invariant(
                    $"Pulling the Ollama model '{ModelName}' failed within {PullBudget.TotalSeconds} seconds: {failure.Message} Logs: {logs}"),
                failure);
        }

        if (pull.ExitCode != 0)
        {
            var logs = await ReadLogsAsync(target, cancellationToken);
            throw new InvalidOperationException(
                FormattableString.Invariant(
                    $"Pulling the Ollama model '{ModelName}' exited {pull.ExitCode}: {pull.Stderr} Logs: {logs}"));
        }
    }

    private static async Task<string> ReadLogsAsync(IContainer target, CancellationToken cancellationToken)
    {
        try
        {
            var logs = await target.GetLogsAsync(default, default, false, cancellationToken);
            return logs.Stdout;
        }
        catch (Exception failure) when (failure is not OperationCanceledException)
        {
            return FormattableString.Invariant($"<logs unavailable: {failure.Message}>");
        }
    }

    private async ValueTask CleanupAsync()
    {
        var owned = container;
        container = null;

        if (owned is null)
        {
            return;
        }

        using var deadline = new CancellationTokenSource(CleanupBudget);

        try
        {
            await owned.DisposeAsync().AsTask().WaitAsync(deadline.Token);
        }
        catch (OperationCanceledException)
        {
            throw new InvalidOperationException(
                FormattableString.Invariant(
                    $"Ollama container '{owned.Id}' cleanup did not complete within {CleanupBudget.TotalSeconds} seconds; remove it manually with 'docker rm -f {owned.Id}'."),
                new TimeoutException());
        }
    }
}
