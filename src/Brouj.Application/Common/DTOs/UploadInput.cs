
namespace Brouj.Application.Common.DTOs;

/// <summary>Explicit Upload Input contract; excludes persistence and credential secrets.</summary>
public sealed class UploadInput
{
    public Stream Content { get; init; } = Stream.Null;

    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public long Length { get; init; }
}
