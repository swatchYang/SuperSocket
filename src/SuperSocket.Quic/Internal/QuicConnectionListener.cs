using System;
using System.Net.Quic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperSocket.Connection;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Connections;
using IConnectionFactory = SuperSocket.Connection.IConnectionFactory;
using IConnectionListener = SuperSocket.Server.Abstractions.Connections.IConnectionListener;

namespace SuperSocket.Quic.Internal;

#pragma warning disable CA2252
internal sealed class QuicConnectionListener(
    ListenOptions listenOptions,
    IServiceProvider provider,
    ILoggerFactory loggerFactory,
    IConnectionFactory connectionFactory,
    ILogger logger) : IConnectionListener
{
    private IMultiplexedConnectionListener _listenSocket;
    private CancellationTokenSource _cancellationTokenSource;
    private TaskCompletionSource<bool> _stopTaskCompletionSource;

    private readonly QuicTransportOptions _transportOptions =
        provider.GetRequiredService<IOptions<QuicTransportOptions>>().Value;

    public IConnectionFactory ConnectionFactory { get; } = connectionFactory;
    public ListenOptions Options => listenOptions;
    public bool IsRunning { get; private set; }

    public bool Start()
    {
        var options = Options;

        try
        {
            var listenEndpoint = options.ToEndPoint();
            var listenCollection = options.ToQuicConnectionFeatureCollection();

            var listenerFactory = QuicListener.CreateListenerFactory(loggerFactory, new QuicTransportOptions
            {
                Backlog = options.BackLog,
                DefaultStreamErrorCode = _transportOptions.DefaultStreamErrorCode,
                MaxBidirectionalStreamCount = _transportOptions.MaxBidirectionalStreamCount,
                MaxReadBufferSize = _transportOptions.MaxReadBufferSize,
                MaxUnidirectionalStreamCount = _transportOptions.MaxUnidirectionalStreamCount,
                MaxWriteBufferSize = _transportOptions.MaxWriteBufferSize,
                DefaultCloseErrorCode = _transportOptions.DefaultCloseErrorCode,
            });

            var listenSocket =
                _listenSocket = listenerFactory.BindAsync(listenEndpoint, listenCollection).GetAwaiter()
                    .GetResult();

            IsRunning = true;

            _cancellationTokenSource = new CancellationTokenSource();

            KeepAcceptAsync(listenSocket, _cancellationTokenSource.Token).DoNotAwait();
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, $"The listener[{this.ToString()}] failed to start.");
            return false;
        }
    }

    private async Task KeepAcceptAsync(IMultiplexedConnectionListener listenSocket, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var quicConnection =
                    await listenSocket.AcceptAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                if (quicConnection == null)
                    break;

                OnNewClientAccept(quicConnection);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Listener[{this.ToString()}] failed to do AcceptAsync");
            }
        }

        _stopTaskCompletionSource.TrySetResult(true);
    }

    public event NewConnectionAcceptHandler NewConnectionAccept;

    private async void OnNewClientAccept(MultiplexedConnectionContext multiplexedConnection)
    {
        var handler = NewConnectionAccept;

        if (handler == null)
            return;

        IConnection connection = null;

        try
        {
            connection = await ConnectionFactory.CreateConnection(multiplexedConnection, CancellationToken.None);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Failed to create quicConnection for {multiplexedConnection.RemoteEndPoint}.");
            return;
        }

        await handler.Invoke(this.Options, connection);
    }

    public async Task StopAsync()
    {
        var listenSocket = _listenSocket;

        if (listenSocket == null)
            return;

        _stopTaskCompletionSource = new TaskCompletionSource<bool>();

        _cancellationTokenSource.Cancel();
        await _listenSocket.DisposeAsync();

        await _stopTaskCompletionSource.Task;
    }

    public override string ToString()
    {
        return Options?.ToString();
    }
}