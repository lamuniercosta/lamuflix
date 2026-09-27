using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.ArchitectureTests.Fixtures.Pipeline;

public sealed record SealedRecordPipelineRequest(string Value);

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

public sealed record UnsealedHandlerPipelineRequest(string Value);

public class UnsealedPipelineCommandHandler : ICommandHandler<UnsealedHandlerPipelineRequest, int>
{
    public Task<int> HandleAsync(UnsealedHandlerPipelineRequest command, CancellationToken cancellationToken) =>
        Task.FromResult(0);
}
