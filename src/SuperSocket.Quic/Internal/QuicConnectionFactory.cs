using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using SuperSocket.Connection;
using SuperSocket.Kestrel.Connection;
using IConnectionFactory = SuperSocket.Connection.IConnectionFactory;

namespace SuperSocket.Quic.Internal;
#pragma warning disable CA2252
internal sealed class QuicConnectionFactory(
    ConnectionOptions connectionOptions)
    : IConnectionFactory
{
    public async Task<IConnection> CreateConnection(object connection, CancellationToken cancellationToken)
    {
        var multiplexedConnectionContext = connection as MultiplexedConnectionContext;

        ArgumentNullException.ThrowIfNull(multiplexedConnectionContext);

        var context = await multiplexedConnectionContext.AcceptAsync(cancellationToken);

        return new KestrelPipeConnection(context, connectionOptions);
    }
}