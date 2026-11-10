using System.Linq;
using LamuFlix.Core.Features.Import;

namespace LamuFlix.Api.Endpoints;

// ReSharper disable NotAccessedPositionalProperty.Global // Serialized by System.Text.Json as the frozen wire shapes, never read in code
internal sealed record ImportCandidatesResponse(string[] NewFolders)
{
    public static ImportCandidatesResponse From(ImportCandidatesResult result) =>
        new([.. result.NewFolders.Select(path => path.Value)]);
}

internal sealed record ImportLibraryResponse(
    int Imported,
    int Skipped,
    int Retried,
    ImportFolderFailureDto[] Failures)
{
    public static ImportLibraryResponse From(ImportLibraryFoldersResult result) =>
        new(
            result.Imported,
            result.Skipped,
            result.Retried,
            [.. result.Failures.Select(ImportFolderFailureDto.From)]);
}

internal sealed record ImportFolderFailureDto(string Folder, string Reason)
{
    public static ImportFolderFailureDto From(ImportLibraryFolderFailure failure) =>
        new(failure.Folder.Value, failure.Reason);
}
// ReSharper restore NotAccessedPositionalProperty.Global
