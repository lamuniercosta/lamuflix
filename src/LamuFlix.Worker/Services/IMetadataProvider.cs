using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Worker.Models;

namespace LamuFlix.Worker.Services
{
    public interface IMetadataProvider
    {
        Task<MovieMetadata?> FetchMetadataAsync(string title, int? year = null, string? imdbId = null, CancellationToken cancellationToken = default);
    }
}
