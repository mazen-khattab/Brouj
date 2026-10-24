using Brouj.Application.Common.Settings;
using Brouj.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace Brouj.Infrastructure.IntegrationTests.Services;

public sealed class FileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "BroujUploadTests", Guid.NewGuid().ToString("N"));
    private static readonly byte[] Png = [137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0, 1, 2, 3];
    private FileStorageService Service(string? root = null) => new(Options.Create(new LocalFileStorageOptions { RootPath = root ?? _root }), Options.Create(new FileStorageOptions()));

    [Theory]
    [InlineData(".png", "image/png")]
    [InlineData(".jpg", "image/jpeg")]
    [InlineData(".jpeg", "image/jpeg")]
    [InlineData(".webp", "image/webp")]
    public async Task Approved_formats_use_generated_names_and_support_idempotent_delete(string extension, string mime)
    {
        var bytes = extension switch
        {
            ".png" => Png,
            ".jpg" or ".jpeg" => new byte[] { 255, 216, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            _ => "RIFF0000WEBPcontents"u8.ToArray()
        };
        var service = Service();
        var name = await service.SaveAsync(new MemoryStream(bytes), "client" + extension, mime, bytes.Length, default);
        Assert.DoesNotContain("client", name);
        Assert.True(Guid.TryParseExact(Path.GetFileNameWithoutExtension(name), "N", out _));
        Assert.Equal(bytes, await File.ReadAllBytesAsync(Path.Combine(_root, name)));
        await service.DeleteAsync(name, default);
        await service.DeleteAsync(name, default);
        Assert.Empty(Directory.GetFiles(_root));
    }

    [Theory]
    [InlineData("../../file.png", "image/png", 15)]
    [InlineData("..\\file.png", "image/png", 15)]
    [InlineData("C:\\file.png", "image/png", 15)]
    [InlineData("file.png:other", "image/png", 15)]
    [InlineData("file.exe", "image/png", 15)]
    [InlineData("file.png", "image/jpeg", 15)]
    [InlineData("file.png", "application/octet-stream", 15)]
    [InlineData("file.png", "image/png", 5242881)]
    [InlineData("file.png", "image/png", 0)]
    [InlineData("file.png\r\nforged-log-entry", "image/png", 15)]
    [InlineData("file.png\0", "image/png", 15)]
    [InlineData("file.svg", "image/svg+xml", 15)]
    public async Task Invalid_metadata_is_rejected_before_creating_files(string name, string mime, long length)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Service().SaveAsync(new MemoryStream(Png), name, mime, length, default));
        Assert.False(Directory.Exists(_root));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public async Task Actual_length_must_match_declared_length_and_failed_writes_are_cleaned(int difference)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Service().SaveAsync(new MemoryStream(Png), "file.png", "image/png", Png.Length + difference, default));
        Assert.Empty(Directory.GetFiles(_root));
    }

    [Fact]
    public async Task Signature_spoofing_is_rejected()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Service().SaveAsync(new MemoryStream(new byte[20]), "file.png", "image/png", 20, default));
        Assert.Empty(Directory.GetFiles(_root));
    }

    [Fact]
    public async Task Read_failure_and_cancellation_remove_partial_files_without_disposing_the_input()
    {
        using var stream = new FailingStream(Png);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service().SaveAsync(stream, "file.png", "image/png", Png.Length, default));
        Assert.Empty(Directory.GetFiles(_root));
        Assert.True(stream.CanRead);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Service().SaveAsync(new MemoryStream(Png), "file.png", "image/png", Png.Length, cancellation.Token));
        Assert.Empty(Directory.GetFiles(_root));
    }

    [Fact]
    public async Task Failure_after_writing_declared_size_still_removes_the_upload()
    {
        using var input = new FailingStream(Png, failAtEnd: true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service().SaveAsync(input, "file.png", "image/png", Png.Length, default));
        Assert.Empty(Directory.GetFiles(_root));
    }

    [Theory]
    [InlineData("../file.png")]
    [InlineData("/file.png")]
    [InlineData("client.png")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.exe")]
    [InlineData("..\\file.png")]
    [InlineData("%2e%2e%2ffile.png")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.png:alternate")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.png/../file.png")]
    public async Task Delete_cannot_address_arbitrary_paths(string path) =>
        await Assert.ThrowsAsync<ArgumentException>(() => Service().DeleteAsync(path, default));

    [Fact]
    public void Storage_root_must_be_absolute_and_outside_executable_and_web_roots()
    {
        Assert.Throws<InvalidOperationException>(() => Service("uploads"));
        Assert.Throws<InvalidOperationException>(() => Service(Path.Combine(AppContext.BaseDirectory, "uploads")));
        Assert.Throws<InvalidOperationException>(() => Service(Path.Combine(_root, "wwwroot", "uploads")));
    }

    [Fact]
    public async Task Non_seekable_upload_cannot_exceed_the_service_limit_by_underreporting_its_length()
    {
        const int maximumBytes = 5 * 1024 * 1024;
        var bytes = new byte[maximumBytes + 1];
        Png.CopyTo(bytes, 0);
        using var input = new NonSeekableStream(bytes);

        // The declared size is allowed; the service must still reject the actual extra byte.
        await Assert.ThrowsAsync<ArgumentException>(() => Service().SaveAsync(
            input,
            "synthetic.png",
            "image/png",
            maximumBytes,
            CancellationToken.None));

        Assert.Empty(Directory.GetFiles(_root));
        Assert.True(input.CanRead);
    }

    [Fact]
    public async Task Configured_smaller_size_limit_also_bounds_streamed_bytes()
    {
        var service = new FileStorageService(
            Options.Create(new LocalFileStorageOptions { RootPath = _root }),
            Options.Create(new FileStorageOptions { MaxFileSizeBytes = 13 }));
        using var input = new NonSeekableStream(Png);

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAsync(
            input,
            "synthetic.png",
            "image/png",
            13,
            CancellationToken.None));

        Assert.Empty(Directory.GetFiles(_root));
    }

    [Fact]
    public async Task Cancellation_after_the_header_is_written_removes_the_partial_upload()
    {
        using var cancellation = new CancellationTokenSource();
        using var input = new CancelAfterHeaderStream(Png, cancellation);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Service().SaveAsync(
            input,
            "synthetic.png",
            "image/png",
            Png.Length,
            cancellation.Token));

        Assert.True(cancellation.IsCancellationRequested);
        Assert.Empty(Directory.GetFiles(_root));
        Assert.True(input.CanRead);
    }

    public void Dispose()
    {
        // This fixture owns exactly this generated temporary directory.
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private sealed class FailingStream : MemoryStream
    {
        private readonly long _failAt;
        public FailingStream(byte[] bytes, bool failAtEnd = false) : base(bytes) => _failAt = failAtEnd ? bytes.Length : 12;
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            Position >= _failAt ? throw new IOException("Synthetic read failure.") :
                base.ReadAsync(buffer[..Math.Min(12, buffer.Length)], cancellationToken);
    }

    private sealed class NonSeekableStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override bool CanSeek => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
    }

    private sealed class CancelAfterHeaderStream(
        byte[] bytes,
        CancellationTokenSource cancellation) : MemoryStream(bytes)
    {
        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            if (Position >= 12)
            {
                cancellation.Cancel();
            }

            // Force the first read to supply only the signature header, then cancel the next read.
            return base.ReadAsync(buffer[..Math.Min(12, buffer.Length)], cancellationToken);
        }
    }
}
