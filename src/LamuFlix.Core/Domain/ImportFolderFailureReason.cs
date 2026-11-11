namespace LamuFlix.Core.Domain;

public sealed record ImportFolderFailureReason
{
    private const string FolderNotFoundCode = "folder-not-found";
    private const string NoVideoCode = "no-video";
    private const string InvalidFolderCode = "invalid-folder";
    private const string ImportFailedCode = "import-failed";

    public static readonly ImportFolderFailureReason FolderNotFound = new(FolderNotFoundCode);

    public static readonly ImportFolderFailureReason NoVideo = new(NoVideoCode);

    public static readonly ImportFolderFailureReason InvalidFolder = new(InvalidFolderCode);

    public static readonly ImportFolderFailureReason ImportFailed = new(ImportFailedCode);

    private ImportFolderFailureReason(string code) => Code = code;

    public string Code { get; }
}
