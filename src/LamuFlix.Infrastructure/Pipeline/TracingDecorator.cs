using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.Infrastructure.Pipeline;

public sealed class TracingDecorator<TReq, TRes>(
    Func<TReq, CancellationToken, Task<TRes>> inner,
    ActivitySource activitySource) : ICommandHandler<TReq, TRes>, IQueryHandler<TReq, TRes>
    where TReq : class
{
    public static readonly ActivitySource Source = new(TelemetryConstants.ActivitySourceName);

    public async Task<TRes> HandleAsync(TReq request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(activitySource);

        var requestType = typeof(TReq).Name;
        using var activity = activitySource.StartActivity(requestType);
        activity?.SetTag(TelemetryConstants.HandlerRequest, requestType);
        try
        {
            return await inner(request, cancellationToken);
        }
        catch (Exception exception)
        {
            MarkError(activity, exception);
            throw;
        }
    }

    private static void MarkError(Activity? activity, Exception exception)
    {
        activity?.SetStatus(ActivityStatusCode.Error);
        activity?.SetTag(TelemetryConstants.ErrorType, exception.GetType().Name);
    }
}
