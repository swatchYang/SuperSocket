using SuperSocket.Connection;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Connections;

namespace SuperSocket.Quic.Internal;

internal sealed class QuicConnectionFactoryBuilder : IConnectionFactoryBuilder
{
    public IConnectionFactory Build(ListenOptions listenOptions, ConnectionOptions connectionOptions)
    {
        return new QuicConnectionFactory(connectionOptions);
    }
}