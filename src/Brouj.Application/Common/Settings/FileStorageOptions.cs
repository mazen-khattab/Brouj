namespace Brouj.Application.Common.Settings;

public sealed class FileStorageOptions
{
    public long MaxFileSizeBytes { get; init; } = 5 * 1024 * 1024;

    public IReadOnlyCollection<string> AllowedExtensions { get; init; }
        = [".jpg", ".jpeg", ".png", ".webp"];

    public IReadOnlyCollection<string> AllowedContentTypes { get; init; }
        = ["image/jpeg", "image/png", "image/webp"];
}
