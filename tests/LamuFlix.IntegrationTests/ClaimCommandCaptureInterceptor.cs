using System.Collections.Concurrent;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LamuFlix.IntegrationTests;

public sealed class ClaimCommandCaptureInterceptor : DbCommandInterceptor
{
    private readonly ConcurrentQueue<string> commands = new();

    public string[] Commands => [.. commands];

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        commands.Enqueue(command.CommandText);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        commands.Enqueue(command.CommandText);
        return ValueTask.FromResult(result);
    }
}