using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;
using Microsoft.Extensions.Logging;

namespace LamuFlix.Infrastructure.Pipeline;

public sealed class LoggingDecorator<TReq, TRes>(
    Func<TReq, CancellationToken, Task<TRes>> inner,
    ILogger logger,
    TimeProvider timeProvider) : ICommandHandler<TReq, TRes>, IQueryHandler<TReq, TRes>
    where TReq : class
{
    public async Task<TRes> HandleAsync(TReq request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var started = timeProvider.GetTimestamp();
        var requestType = typeof(TReq).Name;
        try
        {
            var result = await inner(request, cancellationToken);
            HandlerLog.Completed(logger, requestType, Elapsed(started));
            return result;
        }
        catch (ValidationException)
        {
            HandlerLog.ValidationFailed(logger, requestType, Elapsed(started));
            throw;
        }
        catch (Exception exception)
        {
            HandlerLog.Failed(logger, exception, requestType, Elapsed(started));
            throw;
        }
    }

    private double Elapsed(long started) => timeProvider.GetElapsedTime(started).TotalMilliseconds;
}
