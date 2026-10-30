using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace LamuFlix.Infrastructure.Adapters;

internal sealed class MetadataQuotaHandler(IServiceScopeFactory scopeFactory) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        bool admitted;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var gate = scope.ServiceProvider.GetRequiredService<IOmdbRequestGate>();
            try
            {
                admitted = await gate.TryAcquireAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                throw new MetadataQuotaPersistenceException(exception);
            }
        }

        if (!admitted)
        {
            throw new MetadataQuotaExhaustedException();
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
