using System;
using System.Linq;
using System.Net.Security;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic;
using Microsoft.Extensions.DependencyInjection;
using SuperSocket.Quic.Internal;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Connections;
using SuperSocket.Server.Abstractions.Host;
using IConnectionListenerFactory = SuperSocket.Server.Abstractions.Connections.IConnectionListenerFactory;

namespace SuperSocket.Quic;

#pragma warning disable CA2252
public static class QuicServerHostBuilderExtensions
{
    public static ISuperSocketHostBuilder UseQuic(this ISuperSocketHostBuilder hostBuilder)
    {
        return hostBuilder.UseQuic(s => { });
    }

    public static ISuperSocketHostBuilder UseQuic(this ISuperSocketHostBuilder hostBuilder,
        Action<QuicTransportOptions> globalConfigure)
    {
        if (!System.Net.Quic.QuicListener.IsSupported)
            throw new PlatformNotSupportedException("System.Net.Quic is not supported on this platform.");

        return hostBuilder.ConfigureServices((host, services) =>
        {
            services.Configure(globalConfigure);
            services.AddSingleton<IConnectionListenerFactory, QuicConnectionListenerFactory>();
            services.AddSingleton<IConnectionFactoryBuilder, QuicConnectionFactoryBuilder>();
        }) as ISuperSocketHostBuilder;
    }

    public static ISuperSocketHostBuilder<TReceivePackage> UseQuic<TReceivePackage>(
        this ISuperSocketHostBuilder<TReceivePackage> hostBuilder)
    {
        return (hostBuilder as ISuperSocketHostBuilder).UseQuic(s => { }) as ISuperSocketHostBuilder<TReceivePackage>;
    }

    public static IFeatureCollection ToQuicConnectionFeatureCollection(this ListenOptions options)
    {
        ArgumentNullException.ThrowIfNull(options.AuthenticationOptions);

        if (options.AuthenticationOptions == null)
            options.AuthenticationOptions.EnsureCertificate();

        var collection = new FeatureCollection();

        options.AuthenticationOptions.ApplicationProtocols ??= [SslApplicationProtocol.Http3];

        collection.Set(new TlsConnectionCallbackOptions
        {
            ApplicationProtocols = options.AuthenticationOptions.ApplicationProtocols!,
            OnConnection = (context, cancellationToken) => new ValueTask<SslServerAuthenticationOptions>
            (
                new SslServerAuthenticationOptions
                {
                    EnabledSslProtocols = options.AuthenticationOptions.EnabledSslProtocols,
                    CipherSuitesPolicy = options.AuthenticationOptions.CipherSuitesPolicy,
                    AllowRenegotiation = options.AuthenticationOptions.AllowRenegotiation,
                    ServerCertificateContext = options.AuthenticationOptions.ServerCertificateContext,
                    ApplicationProtocols = options.AuthenticationOptions.ApplicationProtocols,
                    ServerCertificate = options.AuthenticationOptions.ServerCertificate,
                    RemoteCertificateValidationCallback =
                        options.AuthenticationOptions.RemoteCertificateValidationCallback,
                    ClientCertificateRequired = options.AuthenticationOptions.ClientCertificateRequired,
                    CertificateChainPolicy = options.AuthenticationOptions.CertificateChainPolicy,
                    EncryptionPolicy = options.AuthenticationOptions.EncryptionPolicy,
                    CertificateRevocationCheckMode = options.AuthenticationOptions.CertificateRevocationCheckMode,
                    ServerCertificateSelectionCallback =
                        options.AuthenticationOptions.ServerCertificateSelectionCallback,
                }
            )
        });

        return collection;
    }
}