using System;
using System.Buffers;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using SuperSocket.Connection;

namespace SuperSocket.WebSocket.Connection;

public class WebSocketPipeConnection : PipeConnection
{
    private ClientWebSocket _socket;
    private readonly WebSocketMessageType _messageType;

    public WebSocketPipeConnection(
        ClientWebSocket socket,
        ConnectionOptions options,
        WebSocketMessageType messageType = WebSocketMessageType.Binary) : base(options)
    {
        _socket = socket;
        _messageType = messageType;
    }

    protected override void OnClosed()
    {
        _socket = null;
        base.OnClosed();
    }

    protected override async ValueTask<int> FillPipeWithDataAsync(Memory<byte> memory,
        CancellationToken cancellationToken)
    {
        if (_socket == null)
            throw new ArgumentNullException(nameof(_socket), "socket is null.");

        return await ReceiveAsync(_socket, memory, cancellationToken)
            .ConfigureAwait(false);
    }

    private async ValueTask<int> ReceiveAsync(ClientWebSocket socket, Memory<byte> memory,
        CancellationToken cancellationToken)
    {
        var result = await socket.ReceiveAsync(memory, cancellationToken).ConfigureAwait(false);
        return result.MessageType == WebSocketMessageType.Close ? 0 : result.Count;
    }

    protected override async ValueTask<int> SendOverIOAsync(ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken)
    {
        if (_socket == null)
            throw new ArgumentNullException(nameof(_socket), "socket is null.");

        if (buffer.IsSingleSegment)
        {
            await _socket.SendAsync(buffer.First, _messageType, true, cancellationToken)
                .ConfigureAwait(false);

            return buffer.First.Length;
        }

        var count = 0;
        var totalLength = 0;

        foreach (var segment in buffer)
        {
            count++;
            totalLength += segment.Length;
            cancellationToken.ThrowIfCancellationRequested();
            await _socket.SendAsync(segment, _messageType, count != buffer.Length, cancellationToken)
                .ConfigureAwait(false);
        }

        return totalLength;
    }

    protected override void Close()
    {
        var socket = _socket;
        if (socket == null)
            return;

        if (Interlocked.CompareExchange(ref _socket, null, socket) != socket)
            return;

        try
        {
            socket.Abort();
        }
        catch
        {
            // ignored
        }
    }

    protected override bool IsIgnorableException(Exception e)
    {
        if (base.IsIgnorableException(e))
            return true;

        if (e is SocketException se)
        {
            if (se.IsIgnorableSocketException())
                return true;
        }

        return false;
    }
}