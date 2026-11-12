using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

public interface ISubtitleSidecarStore
{
    Task<bool> ExistsAsync(LibraryPath folder, string primaryFileName, CancellationToken cancellationToken);

    Task<byte[]> ReadAsync(
        LibraryPath folder,
        string primaryFileName,
        long maxBytes,
        CancellationToken cancellationToken);

    Task<SubtitleSidecarWriteResult> CreateAsync(
        LibraryPath folder,
        string primaryFileName,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken);

    Task EnsurePublishableAsync(
        LibraryPath folder,
        string primaryFileName,
        CancellationToken cancellationToken);

    Task<SubtitlePublishResult> PublishToSubsAsync(
        LibraryPath folder,
        string primaryFileName,
        string languageCode,
        ReadOnlyMemory<byte> content,
        int maxAttempts,
        CancellationToken cancellationToken);
}
