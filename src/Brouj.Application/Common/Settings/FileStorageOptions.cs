namespace Brouj.Application.Common.Settings;

public sealed class FileStorageOptions
{
    public long MaxFileSizeBytes { get; init; }

    public IReadOnlyCollection<string> AllowedExtensions { get; init; }
        = Array.Empty<string>();

    public IReadOnlyCollection<string> AllowedContentTypes { get; init; }
        = Array.Empty<string>();
}
