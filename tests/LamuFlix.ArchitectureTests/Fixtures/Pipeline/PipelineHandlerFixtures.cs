using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.ArchitectureTests.Fixtures.Pipeline;

public sealed record SealedRecordPipelineRequest;

public sealed class SealedRecordRequestCommandHandler : ICommandHandler<SealedRecordPipelineRequest, int>
{
    public Task<int> HandleAsync(SealedRecordPipelineRequest command, CancellationToken cancellationToken) =>
        Task.FromResult(0);
}

public class UnsealedPipelineRequest;

public sealed class UnsealedRequestCommandHandler : ICommandHandler<UnsealedPipelineRequest, int>
{
    public Task<int> HandleAsync(UnsealedPipelineRequest command, CancellationToken cancellationToken) =>
        Task.FromResult(0);
}

public sealed class SealedNonRecordPipelineRequest;

public sealed class SealedNonRecordRequestCommandHandler : ICommandHandler<SealedNonRecordPipelineRequest, int>
{
    public Task<int> HandleAsync(SealedNonRecordPipelineRequest command, CancellationToken cancellationToken) =>
        Task.FromResult(0);
}

public sealed record UnsealedHandlerPipelineRequest;

public class UnsealedPipelineCommandHandler : ICommandHandler<UnsealedHandlerPipelineRequest, int>
{
    public Task<int> HandleAsync(UnsealedHandlerPipelineRequest command, CancellationToken cancellationToken) =>
        Task.FromResult(0);
}

public sealed record SealedRecordPipelineQueryRequest;

public sealed class SealedRecordRequestQueryHandler : IQueryHandler<SealedRecordPipelineQueryRequest, int>
{
    public Task<int> HandleAsync(SealedRecordPipelineQueryRequest query, CancellationToken cancellationToken) =>
        Task.FromResult(0);
}

public class UnsealedPipelineQueryRequest;

public sealed class UnsealedRequestQueryHandler : IQueryHandler<UnsealedPipelineQueryRequest, int>
{
    public Task<int> HandleAsync(UnsealedPipelineQueryRequest query, CancellationToken cancellationToken) =>
        Task.FromResult(0);
}
