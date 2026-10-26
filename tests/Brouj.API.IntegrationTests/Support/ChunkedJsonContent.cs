using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Brouj.API.IntegrationTests.Support;

internal sealed class ChunkedJsonContent : HttpContent
{
    private readonly byte[] _bytes;

    public ChunkedJsonContent(string json)
    {
        _bytes = Encoding.UTF8.GetBytes(json);
        Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => stream.WriteAsync(_bytes).AsTask();

    protected override bool TryComputeLength(out long length)
    {
        length = 0;

        return false;
    }
}
