namespace Brouj.Infrastructure.Storage;

/// <summary>Infrastructure-only local paths; consumers receive relative opaque filenames instead.</summary>
public sealed class LocalFileStorageOptions
{
    /// <summary>Absolute upload directory outside the application and its web root.</summary>
    public string RootPath { get; set; } = "";
}
