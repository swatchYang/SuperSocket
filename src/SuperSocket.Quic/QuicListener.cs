using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SuperSocket.Quic;

#pragma warning disable CA2252
public static class QuicListener
{
    public static IMultiplexedConnectionListenerFactory CreateListenerFactory(ILoggerFactory loggerFactory,
        QuicTransportOptions quicTransportOptions)
    {
        if (!System.Net.Quic.QuicListener.IsSupported)
            throw new PlatformNotSupportedException("System.Net.Quic is not supported on this platform.");
        
        var quicTransportFactory = FindQuicTransportFactory();
        
        var transportOptions = Options.Create(quicTransportOptions);

        return (IMultiplexedConnectionListenerFactory)Activator.CreateInstance(quicTransportFactory,
            loggerFactory, transportOptions);
    }
    
    private static Type FindQuicTransportFactory()
    {
        const string quicTransportFactoryTypeName =
            "Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.QuicTransportFactory";

        var assembly = typeof(QuicTransportOptions).Assembly;
        var connectionFactoryType = assembly.GetType(quicTransportFactoryTypeName);
        return connectionFactoryType ?? throw new NotSupportedException($"not find{quicTransportFactoryTypeName}");
    }
}