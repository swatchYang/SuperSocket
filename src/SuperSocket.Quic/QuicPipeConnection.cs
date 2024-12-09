using System;
using System.IO;
using System.Net;
using System.Net.Quic;
using System.Net.Sockets;
using SuperSocket.Connection;

namespace SuperSocket.Quic;

public class QuicPipeConnection(
    Stream stream,
    EndPoint remoteEndPoint,
    EndPoint localEndPoint,
    ConnectionOptions options)
    : StreamPipeConnection(stream, remoteEndPoint, localEndPoint, options)
{
    public QuicPipeConnection(Stream stream, EndPoint remoteEndPoint, ConnectionOptions options)
        : this(stream, remoteEndPoint, null, options)
    {
    }

    protected override bool IsIgnorableException(Exception e)
    {
        if (base.IsIgnorableException(e))
            return true;

        switch (e)
        {
            case QuicException:
            case SocketException se when se.IsIgnorableSocketException():
                return true;
            default:
                return false;
        }
    }
}