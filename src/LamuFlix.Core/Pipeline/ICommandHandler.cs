using System.Threading;
using System.Threading.Tasks;

namespace LamuFlix.Core.Pipeline;

public interface ICommandHandler<TCommand, TResult>
    where TCommand : class
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
