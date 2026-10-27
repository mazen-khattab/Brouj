using Brouj.Application.Common.Settings;
using Brouj.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace Brouj.Infrastructure.IntegrationTests.Services;

public sealed class Part10FileAbuseTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "BroujPart10Uploads", Guid.NewGuid().ToString("N"));
    private static readonly byte[] Png = [137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0, 1, 2, 3];

    [Theory]
    [InlineData("image.png.exe")]
    [InlineData("image.png.aspx")]
    [InlineData("image.svg")]
    [InlineData("image.png:alternate")]
    [InlineData("image.png\r\nX-Forged: yes")]
    [InlineData("..\\image.png")]
    [InlineData("../image.png")]
    public async Task Dangerous_final_extensions_paths_and_control_characters_are_rejected(string name)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Service().SaveAsync(
            new MemoryStream(Png), name, "image/png", Png.Length, CancellationToken.None));

        Assert.False(Directory.Exists(_root));
    }

    [Fact]
    public async Task Reusing_a_filename_or_an_executable_double_extension_cannot_overwrite_or_create_executable_files()
    {
        var service = Service();
        var first = await service.SaveAsync(new MemoryStream(Png), "image.exe.png", "image/png", Png.Length, CancellationToken.None);
        var otherBytes = Png.Concat(new byte[] { 9 }).ToArray();
        var second = await service.SaveAsync(new MemoryStream(otherBytes), first, "image/png", otherBytes.Length, CancellationToken.None);

        Assert.NotEqual(first, second);
        Assert.Equal(".png", Path.GetExtension(first));
        Assert.DoesNotContain("exe", first);
        Assert.Equal(Png, await File.ReadAllBytesAsync(Path.Combine(_root, first)));
        Assert.Equal(otherBytes, await File.ReadAllBytesAsync(Path.Combine(_root, second)));
        Assert.Equal(2, Directory.GetFiles(_root).Length);
    }

    private FileStorageService Service()
    {
        return new FileStorageService(Options.Create(new LocalFileStorageOptions { RootPath = _root }),
            Options.Create(new FileStorageOptions()));
    }

    public void Dispose()
    {
        // Only this fixture's generated temporary directory may be removed recursively.
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
