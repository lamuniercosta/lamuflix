using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using LamuFlix.Core.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LamuFlix.Infrastructure.Pipeline;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHandler<THandler, TReq, TRes>(this IServiceCollection services)
        where THandler : class
        where TReq : class
    {
        ArgumentNullException.ThrowIfNull(services);

        var isCommand = typeof(ICommandHandler<TReq, TRes>).IsAssignableFrom(typeof(THandler));
        var isQuery = typeof(IQueryHandler<TReq, TRes>).IsAssignableFrom(typeof(THandler));
        if (!isCommand && !isQuery)
        {
            throw new InvalidOperationException(
                $"{typeof(THandler).Name} must implement ICommandHandler or IQueryHandler.");
        }

        services.AddScoped<THandler>();
        if (isCommand)
        {
            services.AddScoped<ICommandHandler<TReq, TRes>>(Compose<THandler, TReq, TRes>);
        }

        if (isQuery)
        {
            services.AddScoped<IQueryHandler<TReq, TRes>>(Compose<THandler, TReq, TRes>);
        }

        return services;
    }

    private static TracingDecorator<TReq, TRes> Compose<THandler, TReq, TRes>(IServiceProvider serviceProvider)
        where THandler : class
        where TReq : class
    {
        var handler = serviceProvider.GetRequiredService<THandler>();
        var validation = new ValidationDecorator<TReq, TRes>(
            (request, cancellationToken) => Invoke<THandler, TReq, TRes>(handler, request, cancellationToken),
            serviceProvider.GetServices<IValidator<TReq>>());
        var logging = new LoggingDecorator<TReq, TRes>(
            validation.HandleAsync,
            CreateLogger(serviceProvider),
            serviceProvider.GetService<TimeProvider>() ?? TimeProvider.System);
        return new TracingDecorator<TReq, TRes>(logging.HandleAsync, PipelineActivity.Source);
    }

    private static Task<TRes> Invoke<THandler, TReq, TRes>(THandler handler, TReq request, CancellationToken cancellationToken)
        where THandler : class
        where TReq : class
    {
        if (handler is ICommandHandler<TReq, TRes> command)
        {
            return command.HandleAsync(request, cancellationToken);
        }

        return ((IQueryHandler<TReq, TRes>)handler).HandleAsync(request, cancellationToken);
    }

    private static ILogger CreateLogger(IServiceProvider serviceProvider) =>
        serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(TelemetryConstants.ActivitySourceName)
        ?? NullLogger.Instance;
}
