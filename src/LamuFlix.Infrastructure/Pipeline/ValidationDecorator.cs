using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.Infrastructure.Pipeline;

public sealed class ValidationDecorator<TReq, TRes>(
    Func<TReq, CancellationToken, Task<TRes>> inner,
    IEnumerable<IValidator<TReq>> validators) : ICommandHandler<TReq, TRes>, IQueryHandler<TReq, TRes>
    where TReq : class
{
    public async Task<TRes> HandleAsync(TReq request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(validators);

        var failures = await CollectFailures(request, cancellationToken);
        if (failures.Count > 0)
        {
            throw new Core.Pipeline.ValidationException(ToReadOnly(failures));
        }

        return await inner(request, cancellationToken);
    }

    private async Task<Dictionary<string, List<string>>> CollectFailures(
        TReq request,
        CancellationToken cancellationToken)
    {
        var failures = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            foreach (var failure in result.Errors)
            {
                AddFailure(failures, failure.PropertyName, failure.ErrorMessage);
            }
        }

        return failures;
    }

    private static void AddFailure(Dictionary<string, List<string>> failures, string propertyName, string message)
    {
        if (!failures.TryGetValue(propertyName, out var messages))
        {
            messages = [];
            failures[propertyName] = messages;
        }

        messages.Add(message);
    }

    private static Dictionary<string, string[]> ToReadOnly(Dictionary<string, List<string>> failures) =>
        failures.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal);
}
