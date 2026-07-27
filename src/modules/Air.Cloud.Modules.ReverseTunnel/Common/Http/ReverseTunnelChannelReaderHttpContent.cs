using System.Net;
using System.Threading.Channels;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Http;

internal sealed class ReverseTunnelChannelReaderHttpContent : HttpContent
{
    private readonly ChannelReader<byte[]> bodyReader;
    private readonly Func<CancellationToken, Task>? onCancelled;
    private int completedOrCancelled;

    public ReverseTunnelChannelReaderHttpContent(ChannelReader<byte[]> bodyReader, Func<CancellationToken, Task>? onCancelled = null)
    {
        this.bodyReader = bodyReader ?? throw new ArgumentNullException(nameof(bodyReader));
        this.onCancelled = onCancelled;
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        return SerializeToStreamAsync(stream, context, CancellationToken.None);
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var chunk in bodyReader.ReadAllAsync(cancellationToken))
            {
                if (chunk.Length == 0)
                {
                    continue;
                }

                await stream.WriteAsync(chunk, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            Interlocked.Exchange(ref completedOrCancelled, 1);
        }
        catch
        {
            await CancelAsync(cancellationToken);
            throw;
        }
    }

    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _ = CancelAsync(CancellationToken.None);
        }

        base.Dispose(disposing);
    }

    private async Task CancelAsync(CancellationToken cancellationToken)
    {
        if (onCancelled is null || Interlocked.Exchange(ref completedOrCancelled, 1) == 1)
        {
            return;
        }

        await onCancelled(cancellationToken);
    }
}
