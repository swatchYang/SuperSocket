using System;
using Microsoft.Extensions.Logging;
using SuperSocket.Connection;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Connections;
using IConnectionListener = SuperSocket.Server.Abstractions.Connections.IConnectionListener;
using IConnectionListenerFactory = SuperSocket.Server.Abstractions.Connections.IConnectionListenerFactory;

namespace SuperSocket.Quic.Internal;

internal sealed class QuicConnectionListenerFactory(
    IServiceProvider provider,
    IConnectionFactoryBuilder connectionFactoryBuilder)
    : IConnectionListenerFactory
{
    public IConnectionListener CreateConnectionListener(ListenOptions options, ConnectionOptions connectionOptions,
        ILoggerFactory loggerFactory)
    {
        connectionOptions.Logger = loggerFactory.CreateLogger(nameof(IConnection));
        var connectionFactoryLogger = loggerFactory.CreateLogger(nameof(QuicConnectionListener));

        var connectionFactory = connectionFactoryBuilder.Build(options, connectionOptions);

        return new QuicConnectionListener(options, provider, loggerFactory, connectionFactory, connectionFactoryLogger);
    }
}