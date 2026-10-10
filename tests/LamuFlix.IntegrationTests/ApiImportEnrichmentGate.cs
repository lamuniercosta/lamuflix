using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace LamuFlix.IntegrationTests;

public sealed class ApiImportEnrichmentGate
{
    private readonly TaskCompletionSource<ProcessEnrichmentCommand> arrival = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<ProcessEnrichmentCommand> Arrival => arrival.Task;

    public void Release() => release.TrySetResult();

    public void SignalArrival(ProcessEnrichmentCommand command) => arrival.TrySetResult(command);

    public Task WaitForArrivalAsync(CancellationToken cancellationToken) =>
        arrival.Task.WaitAsync(cancellationToken);

    public Task WaitForReleaseAsync(CancellationToken cancellationToken) =>
        release.Task.WaitAsync(cancellationToken);

    public void InstallInto(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        var original = services
            .Where(descriptor =>
                descriptor.ServiceType == typeof(ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>))
            .ToArray();
        original.ShouldHaveSingleItem(
            "Expected exactly one ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome> registration.");
        var captured = original[0];
        captured.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        var factory = captured.ImplementationFactory.ShouldNotBeNull();
        Func<IServiceProvider, ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>> innerFactory =
            provider => (ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>)factory(provider);
        services.Remove(captured);
        services.AddScoped<ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>>(
            provider => new GatedHandler(this, provider, innerFactory));
    }

    private sealed class GatedHandler(
        ApiImportEnrichmentGate gate,
        IServiceProvider provider,
        Func<IServiceProvider, ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>> innerFactory)
        : ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>
    {
        public async Task<ProcessEnrichmentOutcome> HandleAsync(
            ProcessEnrichmentCommand command,
            CancellationToken cancellationToken)
        {
            gate.SignalArrival(command);
            await gate.WaitForReleaseAsync(cancellationToken);
            return await innerFactory(provider).HandleAsync(command, cancellationToken);
        }
    }
}
