#nullable enable
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Data.Models;

namespace LamuFlix.Web.Services
{
    public interface IEnrichmentQueuePublisher
    {
        Task PublishAsync(MovieEnrichmentMessage message, CancellationToken cancellationToken = default);
    }
}
