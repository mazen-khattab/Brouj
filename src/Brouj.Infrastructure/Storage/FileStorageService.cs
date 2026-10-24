using System.Buffers;
using Brouj.Application.Abstractions.Storage;
using Brouj.Application.Common.Settings;
using Microsoft.Extensions.Options;

namespace Brouj.Infrastructure.Storage;

/// <summary>Stores allowlisted images under generated names outside executable application directories.</summary>
public sealed class FileStorageService : IFileStorageService
{
    private const long MaximumBytes = 5 * 1024 * 1024;
    private static readonly Dictionary<string, string> Formats = new(StringComparer.OrdinalIgnoreCase)
        { [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png", [".webp"] = "image/webp" };
    private readonly string _root;
    private readonly FileStorageOptions _options;

    /// <summary>Validates the root and the configured subset of the approved image types.</summary>
    public FileStorageService(IOptions<LocalFileStorageOptions> localOptions, IOptions<FileStorageOptions> options)
    {
        _options = options.Value;
        var configuredRoot = localOptions.Value.RootPath;
        if (!Path.IsPathFullyQualified(configuredRoot)) throw new InvalidOperationException("An absolute storage root is required.");
        _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(configuredRoot));
        var applicationRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(AppContext.BaseDirectory));
        if (IsWithin(_root, applicationRoot) || _root.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => segment.Equals("wwwroot", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Uploads must be outside the application and wwwroot directories.");
        if (_options.MaxFileSizeBytes is <= 0 or > MaximumBytes || _options.AllowedExtensions.Count == 0 ||
            _options.AllowedContentTypes.Count == 0 || _options.AllowedExtensions.Any(e => !Formats.ContainsKey(e)) ||
            _options.AllowedContentTypes.Any(m => !Formats.ContainsValue(m)))
            throw new InvalidOperationException("Invalid approved image storage policy.");
        CheckDirectories();
    }

    /// <summary>Validates upload metadata and signature, streams bounded bytes, and removes incomplete writes.</summary>
    /// <returns>A generated relative filename; the input stream remains owned by the caller.</returns>
    public async Task<string> SaveAsync(Stream content, string originalFileName, string contentType, long length, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        var extension = ValidateMetadata(originalFileName, contentType, length);
        cancellationToken.ThrowIfCancellationRequested();
        CheckDirectories();
        try { Directory.CreateDirectory(_root); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        { throw new InvalidOperationException("File storage operation failed."); }
        CheckDirectories();
        // Never derive a path from the uploaded name. CreateNew also prevents overwriting an existing file.
        var name = Guid.NewGuid().ToString("N") + (extension == ".jpeg" ? ".jpg" : extension);
        var path = Path.Combine(_root, name);
        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        var created = false;
        var completed = false;
        try
        {
            var header = new byte[12];
            var headerLength = await content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
            if (!ValidSignature(extension, header.AsSpan(0, headerLength))) throw InvalidUpload();
            await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                bufferSize: 81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                created = true;
                long total = headerLength;
                if (total > length) throw InvalidUpload();
                await output.WriteAsync(header.AsMemory(0, headerLength), cancellationToken);
                int read;
                while ((read = await content.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
                {
                    total += read;
                    // Check streamed bytes as well as the declared length, including non-seekable streams.
                    if (total > _options.MaxFileSizeBytes || total > length) throw InvalidUpload();
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
                if (total != length) throw InvalidUpload();
                await output.FlushAsync(cancellationToken);
            }
            // Completion includes successful disposal/close, not only writing the expected byte count.
            completed = true;
            return name;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Storage diagnostics contain internal paths. Expose only a generic operational error.
            throw new InvalidOperationException("File storage operation failed.");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            // FileStream is disposed before this finally runs, including cancellation or truncated reads.
            // Incomplete files are removed even when the caller's cancellation token is already cancelled.
            if (created && !completed)
            {
                try { File.Delete(path); }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                { throw new InvalidOperationException("Incomplete upload cleanup failed."); }
            }
        }
    }

    /// <summary>Idempotently deletes only a generated storage filename after checking root/path safety.</summary>
    /// <remarks>The operating system exposes no asynchronous file-delete API; this short metadata operation
    /// uses File.Delete rather than scheduling blocking work on another thread.</remarks>
    public Task DeleteAsync(string storedPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsStoredName(storedPath)) throw new ArgumentException("Invalid stored file reference.", nameof(storedPath));
        CheckDirectories();
        var path = Path.Combine(_root, storedPath);
        try
        {
            if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException("Invalid stored file reference.", nameof(storedPath));
            // The validated, flat generated name cannot address arbitrary paths; deletion is idempotent.
            File.Delete(path);
            return Task.CompletedTask;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException("File storage operation failed.");
        }
    }

    private string ValidateMetadata(string name, string mime, long length)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 255 || name.IndexOfAny(['/', '\\', ':']) >= 0 ||
            name.Any(char.IsControl) || name.Contains("..", StringComparison.Ordinal) || length <= 0 || length > _options.MaxFileSizeBytes)
            throw InvalidUpload();
        var extension = Path.GetExtension(name).ToLowerInvariant();
        if (!Formats.TryGetValue(extension, out var expectedMime) || expectedMime != mime ||
            !_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase) || !_options.AllowedContentTypes.Contains(mime))
            throw InvalidUpload();
        return extension;
    }

    private static bool ValidSignature(string extension, ReadOnlySpan<byte> bytes) => extension switch
    {
        ".jpg" or ".jpeg" => bytes.Length >= 3 && bytes[..3].SequenceEqual(new byte[] { 0xff, 0xd8, 0xff }),
        ".png" => bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        ".webp" => bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8),
        _ => false
    };

    private static bool IsStoredName(string? name)
    {
        if (name is null || name.Length < 36) return false;
        var extension = name[32..];
        return extension is ".jpg" or ".png" or ".webp" && Guid.TryParseExact(name[..32], "N", out _) &&
            name[..32] == name[..32].ToLowerInvariant();
    }

    private void CheckDirectories()
    {
        // Reject symbolic links/junctions anywhere along the root. The directory must be writable only
        // by the service account; application checks cannot defeat a privileged local filesystem race.
        try
        {
            for (var directory = new DirectoryInfo(_root); directory is not null; directory = directory.Parent)
                if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("Storage root cannot contain symbolic links or junctions.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        { throw new InvalidOperationException("File storage operation failed."); }
    }

    private static bool IsWithin(string path, string root) => path.Equals(root, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    private static ArgumentException InvalidUpload() => new("Invalid image upload.");
}
