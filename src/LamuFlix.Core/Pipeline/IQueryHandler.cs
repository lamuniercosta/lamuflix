using System.Threading;
using System.Threading.Tasks;

namespace LamuFlix.Core.Pipeline;

public interface IQueryHandler<TQuery, TResult>
    where TQuery : class
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
