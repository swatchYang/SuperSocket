using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SuperSocket.Connection;
using SuperSocket.ProtoBase;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Connections;

namespace SuperSocket.Server.Connection
{
    public class TcpConnectionFactory<TPackageInfo> : TcpConnectionFactoryBase<TPackageInfo>
    {
        public TcpConnectionFactory(
            ListenOptions listenOptions,
            ConnectionOptions connectionOptions,
            Action<Socket> socketOptionsSetter,
            IPipelineFilterFactory<TPackageInfo> pipelineFilterFactory,
            IConnectionStreamInitializersFactory connectionStreamInitializersFactory)
            : base(listenOptions, connectionOptions, socketOptionsSetter, pipelineFilterFactory, connectionStreamInitializersFactory)
        {
            
        }

        public override async Task<IConnection> CreateConnection(object connection, CancellationToken cancellationToken)
        {
            var socket = connection as Socket;

            ApplySocketOptions(socket);

            if (ConnectionStreamInitializers is IEnumerable<IConnectionStreamInitializer> connectionStreamInitializers
                && connectionStreamInitializers.Any())
            {
                var stream = default(Stream);

                foreach (var initializer in connectionStreamInitializers)
                {
                    stream = await initializer.InitializeAsync(socket, stream, cancellationToken);
                }

                return new StreamPipeConnection<TPackageInfo>(stream, socket.RemoteEndPoint, socket.LocalEndPoint, PipelineFilterFactory.Create(socket), ConnectionOptions);
            }

            return new TcpPipeConnection<TPackageInfo>(socket, PipelineFilterFactory.Create(socket), ConnectionOptions);
        }
    }
}